using System.Collections.Generic;
using System.Linq;
using Code.Data;
using Code.Runtime.Modules.Inventory;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.Runtime.UI.Inventory
{
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class SlotView : MonoBehaviour, ISlotView,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image      _icon;
        [SerializeField] private Image      _highlight;
        [SerializeField] private GameObject _pipPrefab;
        [SerializeField] private Sprite     _rootDashSprite;
        [SerializeField] private Sprite     _arrowSprite;
        [SerializeField] private Sprite     _dashSprite;
        [SerializeField] private Sprite     _deadEndSprite;

        [SerializeField, ReadOnly] private Vector2Int _gridPosition;
        [SerializeField] private Canvas     _canvas;
        
        private IInventoryDragController _dragController;
        private ITetrisContainer         _container;
        private IItemTooltipController   _tooltipController;
        private bool                     _isHovered;

        private readonly Dictionary<(Vector2Int, Vector2Int), Image>    _pips      = new();
        private readonly Dictionary<(Vector2Int, Vector2Int), PipState> _pipStates = new();

        public RectTransform RectTransform => (RectTransform)transform;
        public Vector2Int    GridPosition  => _gridPosition;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                Debug.LogWarning("Assign _canvas in Inspector.", this);
            }
        }

        public void Initialize(Vector2Int gridPosition, IInventoryDragController dragController,
            ITetrisContainer container, IItemTooltipController tooltipController)
        {
            _gridPosition      = gridPosition;
            _dragController    = dragController;
            _tooltipController = tooltipController;
            SetContainer(container);
        }

        public void SetContainer(ITetrisContainer container) => _container = container;

        // Drop-shows-tooltip (tooltip-redesign slice 7, §Interaction): after the drag controller places
        // an item, it asks the anchor slot to surface that item's tooltip in the newly-resolved chain
        // context. Reuses the hover path — same _showDelay (0.4s, not instant → no mid-drag flicker),
        // same anchor computation — so there's a single tooltip-request code path.
        public void ShowTooltip() => RequestTooltip();

        public void SetHighlight(SlotHighlight highlight)
        {
            if (_icon.color == Color.clear) return;

            _icon.color = highlight == SlotHighlight.Swap
                ? new Color(1.00f, 0.80f, 0.00f, 1f)
                : Color.white;
        }

        public void SetPipState(Vector2Int connectorSlotPos, Vector2Int connectorDirection, PipState state)
        {
            var key = (connectorSlotPos, connectorDirection);
            if (!_pips.TryGetValue(key, out var pip)) return;
            if (_pipStates.TryGetValue(key, out var current) && current >= state) return;

            _pipStates[key] = state;
            pip.sprite = state switch
            {
                PipState.RootDash => _rootDashSprite,
                PipState.Arrow => _arrowSprite,
                PipState.Dash  => _dashSprite,
                _              => _deadEndSprite,
            };
            //pip.color = Color.white;
        }

        #region Unity Methods
        // ── UGUI event handlers ───────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if( eventData.button != PointerEventData.InputButton.Left) return;
            _tooltipController?.Hide(null);
            _dragController?.OnSlotPointerClick(this, eventData.position);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if( eventData.button != PointerEventData.InputButton.Left) return;
            _tooltipController?.Hide(null);
            _dragController?.OnSlotBeginDrag(this, eventData.position);
        }

        // Required by Unity for OnBeginDrag to fire.
        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
            => _dragController?.OnSlotEndDrag(eventData.position);

        public void OnDrop(PointerEventData eventData)
            => _dragController?.OnSlotDrop(this);
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _dragController?.SetHovered(this);
            RequestTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _dragController?.SetHovered(null);
            _tooltipController?.Hide(ResolveItem());
        }

        private void RequestTooltip()
        {
            var item = ResolveItem();
            if (item == null) return;
            if (!_container.ContentPointer.TryGetValue(_gridPosition, out var anchor)) return;

            var (screenX, onRight) = ComputeTooltipAnchor(item, anchor);
            _tooltipController?.RequestShow(item, _container, screenX, onRight);
        }

        private (float screenX, bool onRight) ComputeTooltipAnchor(ITetrisItem item, Vector2Int anchor)
        {
            var pointers  = item.GetPointers(anchor);
            var cellWorld = Const.InventoryCellSize * transform.lossyScale.x;
            var slotPos   = (Vector2)RectTransform.position;
            var onRight   = slotPos.x > Screen.width * 0.5f;

            int edgeCol = onRight ? pointers.Min(p => p.x) : pointers.Max(p => p.x);
            float x = slotPos.x + (edgeCol - _gridPosition.x + (onRight ? -0.5f : 0.5f)) * cellWorld;

            return (x, onRight);
        }

        private ITetrisItem ResolveItem()
        {
            if (_container == null) return null;
            if (!_container.ContentPointer.TryGetValue(_gridPosition, out var anchor)) return null;
            _container.Contents.TryGetValue(anchor, out var item);
            return item;
        }

        // ── Item display ──────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_icon == null) return;
            _icon.rectTransform.anchorMin        = new Vector2(0, 1);
            _icon.rectTransform.anchorMax        = new Vector2(0, 1);
            _icon.rectTransform.anchoredPosition = Vector2.zero;
        }
        #endregion
        
        public void RefreshView(ITetrisItem item)
        {
            if (_isHovered)
            {
                _tooltipController?.Hide(null);
                RequestTooltip();
            }
            ClearPips();

            var hasItem = item != null;

            if (hasItem)
            {
                var origin = item.GetShapeOrigin();
                var dim    = item.GetVisualDimensions();

                _icon.sprite                         = item.Icon;
                _icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, (int)item.rotation * 90f);
                _icon.rectTransform.pivot            = CalculatePivot(origin, item.GetDimensions(), item.rotation);
                _icon.rectTransform.sizeDelta        = dim * Const.InventoryCellSize;

                BuildPips(item);
            }
            else
            {
                _icon.rectTransform.sizeDelta        = Vector2.one * Const.InventoryCellSize;
                _icon.rectTransform.anchoredPosition = Vector2.zero;
                _icon.rectTransform.pivot            = Vector2.up;
            }
            
            _icon.color = hasItem ? Color.white : Color.clear;
            _canvas.overrideSorting = hasItem;
            _canvas.sortingOrder    = hasItem ? 1 : 0;
        }

        private void BuildPips(ITetrisItem item)
        {
            if (_pipPrefab == null) return;

            foreach (var (slotPos, direction) in item.GetGridConnectors(_gridPosition))
            {
                var pip   = Instantiate(_pipPrefab, transform).GetComponent<Image>();
                var pipRT = pip.rectTransform;

                pipRT.anchorMin = new Vector2(0, 1);
                pipRT.anchorMax = new Vector2(0, 1);
                pipRT.pivot     = new Vector2(0.5f, 0.5f);
                pipRT.sizeDelta = Vector2.one * Const.InventoryCellSize;

                var offset = slotPos - _gridPosition;
                pipRT.anchoredPosition = new Vector2(
                     (offset.x + 0.5f) * Const.InventoryCellSize,
                    -(offset.y + 0.5f) * Const.InventoryCellSize);

                // Atan2(-y, x) converts Y-down grid direction to screen-space euler angle.
                pipRT.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg);

                pip.sprite = _deadEndSprite;
                pip.raycastTarget = false;

                _pips[(slotPos, direction)] = pip;
            }
        }

        private void ClearPips()
        {
            foreach (var pip in _pips.Values)
                if (pip != null) Destroy(pip.gameObject);
            _pips.Clear();
            _pipStates.Clear();
        }

        private static Vector2 CalculatePivot(Vector2Int origin, Vector2Int dims, RotationType rotation) =>
            rotation switch
            {
                RotationType.None   => new Vector2(origin.x / (float)dims.x,       1f - origin.y / (float)dims.y),
                RotationType.CCW90  => new Vector2(1f - origin.y / (float)dims.y,  1f - origin.x / (float)dims.x),
                RotationType.CCW180 => new Vector2(1f - origin.x / (float)dims.x,  origin.y / (float)dims.y),
                RotationType.CCW270 => new Vector2(origin.y / (float)dims.y,       origin.x / (float)dims.x),
                _                  => new Vector2(0.5f, 0.5f),
            };
    }

    public interface ISlotView
    {
        RectTransform RectTransform { get; }
        Vector2Int    GridPosition  { get; }
        void Initialize(Vector2Int gridPosition, IInventoryDragController dragController,
            ITetrisContainer container, IItemTooltipController tooltipController);
        void SetContainer(ITetrisContainer container);
        void ShowTooltip();
        void RefreshView(ITetrisItem item);
        void SetHighlight(SlotHighlight highlight);
        void SetPipState(Vector2Int connectorSlotPos, Vector2Int connectorDirection, PipState state);
    }

    public enum SlotHighlight { None, Swap }
    public enum PipState { DeadEnd, Dash, Arrow, RootDash }
}