using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Data.Items;
using Code.Runtime.Modules.Statistics;
using Submodules.Utility.Tools.ShapeInspector.RectShape;
using UnityEngine;

namespace Code.Runtime.Modules.Inventory
{
    [Serializable]
    public abstract class TetrisItem : AbstractItem, ITetrisItem
    {
        private readonly RectShapeBool _shape;
        private readonly ItemConfig   _config;

        public string       Name       { get; private set; }
        public RotationType rotation   { get; set; }

        protected TetrisItem(ItemConfig config, RotationType rotation) : base(config)
        {
            _shape        = config.Shape;
            _config       = config;
            Name          = config.name;
            this.rotation = rotation;
        }

        // ── Grid placement ────────────────────────────────────────────────

        public override List<Vector2Int> GetPointers(Vector2Int position)
        {
            var normalized = GetNormalizedShape();
            return normalized.Select(p => position + p - GetShapeOrigin(normalized)).ToList();
        }

        private List<Vector2Int> GetNormalizedShape()
        {
            var parts   = _shape.GetVec2Ints();
            var pivot   = parts[0];
            var rotated = parts.Select(p => ApplyRotation(p - pivot, rotation)).ToList();
            var minX    = rotated.Min(p => p.x);
            var minY    = rotated.Min(p => p.y);
            return rotated.Select(p => p - new Vector2Int(minX, minY)).ToList();
        }

        public Vector2Int GetShapeOrigin(List<Vector2Int> normalized = null)
        {
            normalized ??= GetNormalizedShape();
            var minXInTopRow = normalized.Where(p => p.y == 0).Min(p => p.x);
            return new Vector2Int(minXInTopRow, 0);
        }

        public Vector2Int GetDimensions()
        {
            var normalized = GetNormalizedShape();
            var width      = normalized.Max(p => p.x) - normalized.Min(p => p.x) + 1;
            var height     = normalized.Max(p => p.y) - normalized.Min(p => p.y) + 1;
            return new Vector2Int(width, height);
        }
        
        public Vector2Int GetVisualDimensions()
        {
            var dims         = GetDimensions();
            var isTransposed = rotation is RotationType.CCW90 or RotationType.CCW270;
            return isTransposed ? new Vector2Int(dims.y, dims.x) : dims;
        }

        // ── Chain connectors ──────────────────────────────────────────────

        public List<(Vector2Int slotPos, Vector2Int direction)> GetGridConnectors(Vector2Int placement)
        {
            var parts  = _shape.GetVec2Ints();
            var pivot  = parts[0];

            var rotatedCells = new List<Vector2Int>(parts.Count);
            foreach (var p in parts)
                rotatedCells.Add(ApplyRotation(p - pivot, rotation));

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            foreach (var p in rotatedCells)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
            }
            var normOffset = new Vector2Int(minX, minY);

            var origin = GetShapeOrigin();
            var result = new List<(Vector2Int, Vector2Int)>(_config.Connectors.Count);

            foreach (var connector in _config.Connectors)
            {
                var rotatedPos = ApplyRotation(connector.position - pivot, rotation) - normOffset;
                var rotatedDir = ApplyRotation(connector.direction.ToVector2Int(), rotation);
                var gridPos    = placement + rotatedPos - origin;
                result.Add((gridPos, rotatedDir));
            }

            return result;
        }

        // ── Rotation ──────────────────────────────────────────────────────

        protected Vector2Int ApplyRotation(Vector2Int v, RotationType rot)
        {
            var rotations = (int)rot;
            for (var i = 0; i < rotations; i++)
                v = new Vector2Int(v.y, -v.x);
            return v;
        }
    }

    public interface ITetrisItem : IItem
    {
        string       Name       { get; }
        RotationType rotation   { get; set; }

        List<Vector2Int> GetPointers(Vector2Int position);
        Vector2Int       GetShapeOrigin(List<Vector2Int> normalized = null);
        Vector2Int       GetDimensions();
        Vector2Int GetVisualDimensions();

        List<(Vector2Int slotPos, Vector2Int direction)> GetGridConnectors(Vector2Int placement);
    }

    // Base class for components, not for weapons. Weapons do not apply statMods but attack
    public abstract class AttachmentItem : TetrisItem, IAttachmentItem
    {
        protected AttachmentItem(AttachmentItemConfig config, RotationType rotation) : base(config, rotation)
        {
            // PawnStat.None is the "no passive" sentinel — the default for an unset pawnStatMod.
            // Don't fabricate a no-op affix for it: keep affixes empty so OnUnchained does nothing
            // and the tooltip shows no phantom "unchained: None" line.
            if (config.pawnStatMod.stat != PawnStat.None)
                _affixes.Add(new PawnStatModifier(config.pawnStatMod.stat,
                    new Modifier(config.pawnStatMod.value, config.pawnStatMod.type, Guid)));
        }

        public IReadOnlyList<PawnStatModifier> affixes => _affixes;
        private readonly List<PawnStatModifier> _affixes = new();
        
        public void OnUnchained(IPawnStats stats)
        {
            if (_affixes.Count == 0) return;
            foreach (var affix in _affixes)
                stats.ApplyMod(affix);
        }

        public void OnChained(IPawnStats stats)
        {
            if (_affixes.Count == 0) return;
            foreach (var affix in _affixes)
                stats.RemoveMod(affix);
        }
    }
    
    public interface IAttachmentItem
    {
        IReadOnlyList<PawnStatModifier> affixes { get; }
   
        void OnUnchained(IPawnStats stats);
        void OnChained(IPawnStats stats);
    }

    [Serializable]
    public enum RotationType
    {
        None   = 0,
        CCW90  = 1,
        CCW180 = 2,
        CCW270 = 3,
    }
}