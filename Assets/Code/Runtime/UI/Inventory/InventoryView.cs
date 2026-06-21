using System.Collections.Generic;
using System.Linq;
using Code.Data;
using Code.Runtime.Modules.Inventory;
using NaughtyAttributes;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.UI.Inventory
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class InventoryView : MonoBehaviour, IInventoryView
    {
        [SerializeField] private SlotView                _slotPrefab;
        [SerializeField] private GridLayoutGroup         _grid;
        [SerializeField] private ChainOverlayView        _chainOverlay;
        [SerializeField] private InventoryDragController _dragController;
        [SerializeField] private ItemTooltipController _tooltipController;

        [SerializeField, ReadOnly, AllowNesting] private SlotView[] _slots;

        public IReadOnlyList<ISlotView> Slots => _slots.ToList();

        private ITetrisContainer _container;
        private Vector2Int       _builtForSize;

        private void Awake()
        {
            if (_grid == null)
            {
                _grid = GetComponent<GridLayoutGroup>();
                Debug.LogWarning("Assign _grid in Inspector.", this);
            }
            
            _grid.cellSize       = Const.InventoryCellSize.ToVector2();
            _grid.spacing        = Vector2.zero;
            _grid.padding.left   = Const.InventoryPadding;
            _grid.padding.right  = Const.InventoryPadding;
            _grid.padding.top    = Const.InventoryPadding;
            _grid.padding.bottom = Const.InventoryPadding;
        }

        private void OnEnable()
        {
            if (_container != null)
                _dragController?.Register(_container, Slots);
        }

        private void OnDisable()
        {
            if (_container != null)
                _dragController?.Unregister(_container);
        }

        private void OnDestroy()
        {
            if (_container != null)
                _container.OnContentsChanged -= OnContentsChanged;
        }

        public void RefreshView(ITetrisContainer container)
        {
            if (_container != container)
            {
                if (_container != null)
                {
                    _container.OnContentsChanged -= OnContentsChanged;
                    _dragController?.Unregister(_container);
                }

                _container = container;
                
                _container.OnContentsChanged += OnContentsChanged;
                _chainOverlay?.Bind(_container);

                if (_slots != null)
                    foreach (var slot in _slots)
                        slot.SetContainer(container);
            }

            RebuildSlotsIfNeeded(_container.GridSize);
            _dragController?.Register(_container, Slots);
            Refresh();
        }

        private void RebuildSlotsIfNeeded(Vector2Int gridSize)
        {
            var required = gridSize.x * gridSize.y;
            if (_slots != null && _slots.Length == required && _builtForSize == gridSize)
                return;

            if (_slots != null)
                foreach (var slot in _slots)
                    if (slot != null) Destroy(slot.gameObject);

            _grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = gridSize.x;

            _slots = new SlotView[required];
            for (var i = 0; i < required; i++)
            {
                var gridPos = new Vector2Int(i % gridSize.x, i / gridSize.x);
                var slot    = Instantiate(_slotPrefab, _grid.transform);
                slot.Initialize(gridPos, _dragController, _container, _tooltipController);
                _slots[i] = slot;
            }

            _builtForSize = gridSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_grid.transform);
        }

        private void OnContentsChanged(
            IReadOnlyDictionary<Vector2Int, ITetrisItem> _) => Refresh();

        private void Refresh()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                var pos = new Vector2Int(
                    i % _container.GridSize.x,
                    i / _container.GridSize.x);

                _slots[i].RefreshView(
                    _container.Contents.TryGetValue(pos, out var item) ? item : null);
            }

            var topology = _container.Topology;

            foreach (var (anchor, item) in _container.Contents)
            {
                var slotIndex = anchor.y * _container.GridSize.x + anchor.x;
                if (slotIndex < 0 || slotIndex >= _slots.Length) continue;

                if (topology.DownstreamConnectors.TryGetValue(item, out var downstreamSet))
                    foreach (var (slotPos, direction) in downstreamSet)
                        _slots[slotIndex].SetPipState(slotPos, direction, PipState.Arrow);

                if (topology.UpstreamConnectors.TryGetValue(item, out var upstreamSet))
                    foreach (var (slotPos, direction) in upstreamSet)
                        _slots[slotIndex].SetPipState(slotPos, direction, PipState.Dash);

                if (topology.Roots.Contains(item))
                {
                    var itemPos = _container.Contents.First(kvp => kvp.Value == item).Key;
                    foreach (var (slotPos, direction) in item.GetGridConnectors(itemPos))
                        _slots[slotIndex].SetPipState(slotPos, direction, PipState.RootDash);
                }
            }
        }
    }

    public interface IInventoryView
    {
        IReadOnlyList<ISlotView> Slots { get; }
        void RefreshView(ITetrisContainer container);
    }
}