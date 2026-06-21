using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Code.Runtime.Modules.Inventory
{
    [Serializable]
    public sealed class TetrisContainer : ITetrisContainer
    {
        public TetrisContainer(Vector2Int gridSize)
        {
            GridSize = gridSize;
        }

        private readonly Dictionary<Vector2Int, ITetrisItem> _contents       = new();
        private readonly Dictionary<Vector2Int, Vector2Int>  _contentPointer = new();

        public IReadOnlyDictionary<Vector2Int, ITetrisItem> Contents       => _contents;
        public IReadOnlyDictionary<Vector2Int, Vector2Int>  ContentPointer => _contentPointer;

        public Vector2Int GridSize { get; }

        private ChainTopology _topology;
        private bool          _topologyDirty = true;

        /// <summary>Resolved once and cached; recomputed lazily after the next content change. A
        /// multi-step mutation (e.g. a swap = remove + add) therefore resolves once, and every
        /// consumer reads the same instance — no per-hover or per-frame re-resolve.</summary>
        public ChainTopology Topology
        {
            get
            {
                if (_topologyDirty)
                {
                    _topology      = ChainResolver.ResolveTopology(this);
                    _topologyDirty = false;
                }
                return _topology;
            }
        }

        public event Action<IReadOnlyDictionary<Vector2Int, ITetrisItem>> OnContentsChanged;

        public bool TryAdd(ITetrisItem arrival)
        {
            if (arrival == null)
                return false;

            for (var x = 0; x < GridSize.x; x++)
                for (var y = 0; y < GridSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (CanAddAt(position, arrival, out var other) && other.Count == 0)
                        if (TryAddAt(position, ref arrival))
                            return true;
                }

            return false;
        }

        public bool TryAddAt(Vector2Int position, ref ITetrisItem arrival)
        {
            if (!CanAddAt(position, arrival, out var other))
                return false;

            if (other.Count == 0)
                return Add(position, arrival);

            if (!TryRemove(other[0], out var removed))
            {
                Debug.LogError($"Failed to remove at {other[0]}");
                return false;
            }

            if (!Add(position, arrival))
            {
                Add(other[0], removed);
                return false;
            }

            arrival = removed;
            return true;
        }

        private bool Add(Vector2Int position, ITetrisItem arrival)
        {
            var pointers      = arrival.GetPointers(position);
            var addedPointers = new List<Vector2Int>();

            foreach (var pointer in pointers)
            {
                if (!_contentPointer.TryAdd(pointer, position))
                {
                    foreach (var added in addedPointers)
                        _contentPointer.Remove(added);

                    Debug.LogWarning($"Failed to add item at {position}, cell {pointer} already occupied.");
                    return false;
                }
                addedPointers.Add(pointer);
            }

            if (!_contents.TryAdd(position, arrival))
            {
                foreach (var added in addedPointers)
                    _contentPointer.Remove(added);

                Debug.LogWarning($"Failed to add item to Contents at {position}.");
                return false;
            }

            _topologyDirty = true;
            OnContentsChanged?.Invoke(Contents);
            return true;
        }

        public bool TryRemove(ITetrisItem toRemove) => _contents.Keys.Any(position =>
            _contents[position] == toRemove && TryRemove(position, out _));

        public bool TryRemove(Vector2Int position, out ITetrisItem removed)
        {
            if (IsEmpty(position))
            {
                removed = null;
                return false;
            }

            return Remove(position, out removed);
        }

        private bool Remove(Vector2Int position, out ITetrisItem removed)
        {
            if (!_contents.TryGetValue(position, out removed))
                return false;

            if (!_contents.Remove(position))
                return false;

            var pointers = removed.GetPointers(position);
            foreach (var pointer in pointers)
                _contentPointer.Remove(pointer);

            _topologyDirty = true;
            OnContentsChanged?.Invoke(Contents);
            return true;
        }

        public bool CanAddAt(Vector2Int position, ITetrisItem item, out List<Vector2Int> overlapping)
        {
            var pointers = item.GetPointers(position);
            if (pointers.Any(pointer => !IsValidPointer(pointer)))
            {
                overlapping = null;
                return false;
            }

            overlapping = new List<Vector2Int>();

            foreach (var pointer in pointers)
                if (IsOccupied(pointer, out var contentKey))
                    overlapping.Add(contentKey);

            overlapping = overlapping.Distinct().ToList();
            return overlapping.Count <= 1;
        }

        private bool IsEmpty(Vector2Int pointer)    => IsValidPointer(pointer) && !_contentPointer.ContainsKey(pointer);
        private bool IsOccupied(Vector2Int pointer, out Vector2Int contentKey) => _contentPointer.TryGetValue(pointer, out contentKey);
        private bool IsValidPointer(Vector2Int pointer) => 0 <= pointer.x && pointer.x < GridSize.x &&
                                                           0 <= pointer.y && pointer.y < GridSize.y;
    }

    public interface ITetrisContainer
    {
        Vector2Int GridSize { get; }
        IReadOnlyDictionary<Vector2Int, ITetrisItem> Contents       { get; }
        IReadOnlyDictionary<Vector2Int, Vector2Int>  ContentPointer { get; }

        /// <summary>The resolved chain topology for the current contents. Owned and cached by the
        /// container — resolved once per content change, read by all consumers (no per-hover re-resolve).</summary>
        ChainTopology Topology { get; }

        event Action<IReadOnlyDictionary<Vector2Int, ITetrisItem>> OnContentsChanged;

        bool TryAdd(ITetrisItem item);
        bool TryAddAt(Vector2Int position, ref ITetrisItem arrival);
        bool TryRemove(Vector2Int position, out ITetrisItem removed);
        bool TryRemove(ITetrisItem item);
        bool CanAddAt(Vector2Int position, ITetrisItem item, out List<Vector2Int> overlapping);
    }
}