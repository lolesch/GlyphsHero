using System.Collections.Generic;
using Code.Data;
using Code.Runtime.Modules.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.UI.Inventory
{
    [RequireComponent(typeof(Canvas))]
    public sealed class InventoryDragController : MonoBehaviour, IInventoryDragController
    {
        [SerializeField] private Image       _ghostImage;
        [SerializeField] private Canvas      _canvas;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   pickupClip, dropClip;

        private sealed class ContainerBinding
        {
            public IReadOnlyList<ISlotView> Slots;
        }

        private readonly Dictionary<ITetrisContainer, ContainerBinding> _bindings        = new();
        private readonly Dictionary<ISlotView, ITetrisContainer>        _slotToContainer = new();

        private ITetrisContainer _sourceContainer;
        private ISlotView        _hoveredSlot;

        private ITetrisItem _heldItem;
        private Vector2Int  _grabOffset;
        private Vector2Int  _grabOrigin;
        private Vector2Int  _pickupAnchor;
        private Vector2     _grabSubCellOffset;

        private bool _dropProcessed;

        private enum GestureMode { Idle, Click, Drag }
        private GestureMode _gesture;
        
        private void Awake()
        {
            if (_ghostImage == null)
                Debug.LogWarning("Assign _ghostImage in Inspector.", this);
            
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                Debug.LogWarning("Assign _canvas in Inspector.", this);
            }
        }
        
        private void Start()
        {
            if (_ghostImage != null)
                _ghostImage.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_heldItem == null) return;

            UpdateGhostPosition(Input.mousePosition);
            UpdatePreview();

            if (Input.GetKeyDown(KeyCode.Q))      Rotate(clockwise: false);
            if (Input.GetKeyDown(KeyCode.E))      Rotate(clockwise: true);
            if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
        }

        public void Register(ITetrisContainer container, IReadOnlyList<ISlotView> slots)
        {
            if (_bindings.ContainsKey(container))
                Unregister(container);

            _bindings[container] = new ContainerBinding { Slots = slots };

            foreach (var slot in slots)
                _slotToContainer[slot] = container;
        }

        public void Unregister(ITetrisContainer container)
        {
            if (!_bindings.TryGetValue(container, out var binding))
                return;

            foreach (var slot in binding.Slots)
                _slotToContainer.Remove(slot);

            _bindings.Remove(container);

            if (_heldItem != null && _sourceContainer == container)
                Cancel();
        }

        public void OnSlotPointerClick(ISlotView slot, Vector2 screenPos)
        {
            if (_gesture == GestureMode.Idle &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                 Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                TryTransferToOtherContainer(slot, screenPos);
                return;
            }

            switch (_gesture)
            {
                case GestureMode.Idle:
                    if (TryPickUp(slot, screenPos))
                        _gesture = GestureMode.Click;
                    break;

                case GestureMode.Click:
                    DropAt(slot);
                    break;
            }
        }

        public void OnSlotBeginDrag(ISlotView slot, Vector2 screenPos)
        {
            _dropProcessed = false;

            switch (_gesture)
            {
                case GestureMode.Idle:
                    if (TryPickUp(slot, screenPos))
                        _gesture = GestureMode.Drag;
                    break;

                case GestureMode.Click:
                    _gesture = GestureMode.Drag;
                    break;
            }
        }

        public void OnSlotEndDrag(Vector2 screenPos)
        {
            if (_gesture != GestureMode.Drag) return;

            if (_dropProcessed)
            {
                _dropProcessed = false;
                return;
            }

            Cancel();
        }

        public void OnSlotDrop(ISlotView slot)
        {
            if (_gesture != GestureMode.Drag) return;

            _dropProcessed = true;
            DropAt(slot);
        }

        public void SetHovered(ISlotView slot)
        {
            _hoveredSlot = slot;
            UpdatePreview();
        }

        public void Cancel()
        {
            if (_heldItem == null) return;

            ITetrisItem returning = _heldItem;
            _sourceContainer.TryAddAt(_pickupAnchor, ref returning);

            if (ReferenceEquals(returning, _heldItem))
                EndDrag();
            else
                ContinueHolding(returning);
        }
       
        private void TryTransferToOtherContainer(ISlotView slot, Vector2 screenPos)
        {
            if (!_slotToContainer.TryGetValue(slot, out var source)) return;
            if (!source.ContentPointer.TryGetValue(slot.GridPosition, out var anchor)) return;
            if (!source.Contents.TryGetValue(anchor, out var item)) return;

            var target = FindOtherContainer(source);
            if (target != null)
            {
                ITetrisItem transfer = item;
                if (target.TryAdd(transfer))
                {
                    source.TryRemove(anchor, out _);
                    
                    audioSource.PlayOneShot(dropClip);
                    return;
                }
            }

            // Transfer failed or no target — pick up the item instead.
            if (TryPickUp(slot, screenPos))
                _gesture = GestureMode.Click;
        }

        private ITetrisContainer FindOtherContainer(ITetrisContainer source)
        {
            foreach (var container in _bindings.Keys)
                if (container != source) return container;
            return null;
        }

        private bool TryPickUp(ISlotView slot, Vector2 screenPos)
        {
            if (!_slotToContainer.TryGetValue(slot, out var container)) return false;

            var clickedCell = slot.GridPosition;

            if (!container.ContentPointer.TryGetValue(clickedCell, out var anchor)) return false;
            if (!container.TryRemove(anchor, out var item))                          return false;

            _heldItem        = item;
            _sourceContainer = container;
            _pickupAnchor    = anchor;
            _grabOffset      = clickedCell - anchor;
            _grabOrigin      = item.GetShapeOrigin();

            _grabSubCellOffset = (Vector2)slot.RectTransform.position - screenPos;

            RefreshGhostImage();
            _ghostImage.gameObject.SetActive(true);
            return true;
        }

        private void DropAt(ISlotView slot)
        {
            if (!_slotToContainer.TryGetValue(slot, out var targetContainer)) { Cancel(); return; }

            var targetAnchor = slot.GridPosition - _grabOffset;

            // Same- and cross-container drops share one rule: place the held item, return any single
            // displaced item to the freed source cell, and only force-pickup it if it won't fit there.
            ITetrisItem carried = _heldItem;
            if (!targetContainer.TrySwapInto(targetAnchor, ref carried, _sourceContainer, _pickupAnchor))
            {
                Cancel();
                return;
            }

            if (carried == null)
            {
                EndDrag();
            }
            else
            {
                ContinueHolding(carried, targetContainer);
                _pickupAnchor = targetAnchor; // displaced item now lives where the drop happened
            }
        }

        private void ContinueHolding(ITetrisItem item, ITetrisContainer newSource = null)
        {
            _heldItem          = item;
            if (newSource != null) _sourceContainer = newSource;
            _grabOrigin        = _heldItem.GetShapeOrigin();
            var dims           = _heldItem.GetDimensions();
            _grabOffset        = new Vector2Int(dims.x / 2, dims.y / 2) - _grabOrigin;
            _grabSubCellOffset = Vector2.zero;
            _gesture           = GestureMode.Click;
            
            audioSource.PlayOneShot(dropClip);
            RefreshGhostImage();
        }

        private void EndDrag()
        {
            _heldItem        = null;
            _sourceContainer = null;
            _gesture         = GestureMode.Idle;
            _ghostImage.gameObject.SetActive(false);
            ClearAllHighlights();
            
            audioSource.PlayOneShot(dropClip);
        }

        private void Rotate(bool clockwise)
        {
            var dims    = _heldItem.GetDimensions();
            var pClicked = _grabOffset + _grabOrigin;

            var pNew = clockwise
                ? new Vector2Int(dims.y - 1 - pClicked.y, pClicked.x)
                : new Vector2Int(pClicked.y, dims.x - 1 - pClicked.x);

            var steps = clockwise ? 3 : 1;
            _heldItem.rotation = (RotationType)(((int)_heldItem.rotation + steps) % 4);

            _grabOrigin = _heldItem.GetShapeOrigin();
            _grabOffset = pNew - _grabOrigin;

            _grabSubCellOffset = clockwise
                ? new Vector2( _grabSubCellOffset.y, -_grabSubCellOffset.x)
                : new Vector2(-_grabSubCellOffset.y,  _grabSubCellOffset.x);

            RefreshGhostImage();
        }

        private void RefreshGhostImage()
        {
            if (_heldItem == null || _ghostImage == null) return;

            var visual = _heldItem.GetVisualDimensions();

            var ghostRT              = _ghostImage.rectTransform;
            ghostRT.pivot            = new Vector2(0.5f, 0.5f);
            ghostRT.sizeDelta        = new Vector2( visual.x, visual.y) * Const.InventoryCellSize;
            ghostRT.localEulerAngles = new Vector3(0f, 0f, (int)_heldItem.rotation * 90f);

            _ghostImage.sprite = _heldItem.Icon;
            _ghostImage.color  = new Color(1f, 1f, 1f, 0.70f);
            
            audioSource.PlayOneShot(pickupClip);
        }

        private void UpdateGhostPosition(Vector2 screenPos)
        {
            if (_heldItem == null || _ghostImage == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, screenPos, null, out var canvasLocal);

            _ghostImage.rectTransform.anchoredPosition =
                canvasLocal - GrabFromCenter() + _grabSubCellOffset / _canvas.scaleFactor;
        }

        private Vector2 GrabFromCenter()
        {
            var dims = _heldItem.GetDimensions();
            return new Vector2(
                _grabOffset.x + _grabOrigin.x + 0.5f - dims.x * 0.5f, 
                dims.y * 0.5f - _grabOffset.y - _grabOrigin.y - 0.5f) * Const.InventoryCellSize;
        }

        private void UpdatePreview()
        {
            ClearAllHighlights();

            if (_heldItem == null) return;

            if (_hoveredSlot == null || !_slotToContainer.TryGetValue(_hoveredSlot, out var targetContainer))
            {
                _ghostImage.color = new Color(1f, 0.40f, 0.40f, 0.70f);
                return;
            }

            var targetAnchor = _hoveredSlot.GridPosition - _grabOffset;
            var canAdd       = targetContainer.CanAddAt(targetAnchor, _heldItem, out var overlapping);

            if (!canAdd)
            {
                _ghostImage.color = new Color(1f, 0.40f, 0.40f, 0.70f);
                return;
            }

            if (overlapping is { Count: 1 } &&
                targetContainer.Contents.TryGetValue(overlapping[0], out var swapItem))
            {
                //HighlightCells(targetContainer, overlapping[0], SlotHighlight.Swap, swapItem);
                _ghostImage.color = new Color(1.00f, 0.80f, 0.00f, 0.70f);
                return;
            }

            _ghostImage.color = new Color(1f, 1f, 1f, 0.70f);
        }

        private void HighlightCells(ITetrisContainer container, Vector2Int anchor,
            SlotHighlight highlight, ITetrisItem item = null)
        {
            item ??= _heldItem;
            foreach (var ptr in item.GetPointers(anchor))
                GetSlotAt(container, ptr)?.SetHighlight(highlight);
        }

        private void ClearAllHighlights()
        {
            foreach (var binding in _bindings.Values)
                foreach (var slot in binding.Slots)
                    slot.SetHighlight(SlotHighlight.None);
        }

        private ISlotView GetSlotAt(ITetrisContainer container, Vector2Int gridPos)
        {
            if (!_bindings.TryGetValue(container, out var binding)) return null;

            if (gridPos.x < 0 || gridPos.x >= container.GridSize.x ||
                gridPos.y < 0 || gridPos.y >= container.GridSize.y)
                return null;

            var index = gridPos.y * container.GridSize.x + gridPos.x;
            return index < binding.Slots.Count ? binding.Slots[index] : null;
        }
    }

    public interface IInventoryDragController
    {
        void Register(ITetrisContainer container, IReadOnlyList<ISlotView> slots);
        void Unregister(ITetrisContainer container);
        void OnSlotPointerClick(ISlotView slot, Vector2 screenPos);
        void OnSlotBeginDrag(ISlotView  slot,   Vector2 screenPos);
        void OnSlotEndDrag(Vector2 screenPos);
        void OnSlotDrop(ISlotView slot);
        void SetHovered(ISlotView slot);
        void Cancel();
    }
}