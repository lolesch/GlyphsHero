using System.Collections.Generic;
using Code.Runtime.Modules.Inventory;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.UI.Inventory
{
    [RequireComponent(typeof(InventoryView))]
    public sealed class ChainOverlayView : MonoBehaviour
    {
        [SerializeField] private InventoryView _inventoryView;

        private ITetrisContainer                  _container;
        private IReadOnlyList<ISlotView>          _slots;

        private static readonly Color ColorConnected   = Color.yellow;
        private static readonly Color ColorUnconnected = Color.red;
        private static readonly Color ColorDot         = Color.white;

        private const float DotRadius   = 4f;
        private const float ArrowLength = 0.5f;
        
        private void Awake()
        {
            if (_inventoryView == null)
            {
                _inventoryView = GetComponent<InventoryView>();
                Debug.LogWarning("Assign _inventoryView in Inspector.", this);
            }
        }
        public void Bind(ITetrisContainer container)
        {
            _container = container;
            _slots     = _inventoryView.Slots;
        }

        private void OnDrawGizmos()
        {
            if (_container == null || _slots == null) return;

            // The container owns the resolved topology (cached, resolved once per content change);
            // the overlay reads it rather than receiving a pushed copy.
            var topology = _container.Topology;

            var o = GetWorldPos(new Vector2Int(0, 0));
            var r = GetWorldPos(new Vector2Int(1, 0));
            var d = GetWorldPos(new Vector2Int(0, 1));

            if (!o.HasValue) return;

            var cellSize   = r.HasValue ? Vector2.Distance(o.Value, r.Value)
                           : d.HasValue ? Vector2.Distance(o.Value, d.Value)
                           : 30f;
            var worldRight = r.HasValue ? (r.Value - o.Value).normalized : Vector2.right;
            var worldDown  = d.HasValue ? (d.Value - o.Value).normalized : Vector2.down;

            foreach (var kvp in _container.Contents)
            {
                var item       = kvp.Value;
                var pos        = kvp.Key;
                var connectors = item.GetGridConnectors(pos);

                for (var i = 0; i < connectors.Count; i++)
                {
                    for (var j = i + 1; j < connectors.Count; j++)
                    {
                        var (slotA, dirA) = connectors[i];
                        var (slotB, dirB) = connectors[j];

                        if (slotA == slotB) continue;

                        var worldA = GetWorldPos(slotA);
                        var worldB = GetWorldPos(slotB);

                        if (!worldA.HasValue || !worldB.HasValue) continue;

                        var dist      = Vector2.Distance(worldA.Value, worldB.Value);
                        var handleLen = dist * 0.5f;
                        var wDirA     = (dirA.x * worldRight + dirA.y * worldDown).normalized;
                        var wDirB     = (dirB.x * worldRight + dirB.y * worldDown).normalized;
                        var p0 = worldA.Value + wDirA * DotRadius;
                        var p3 = worldB.Value + wDirB * DotRadius;
                        var p1 = p0 - wDirA * handleLen;
                        var p2 = p3 - wDirB * handleLen;

                        Gizmos.color = ColorDot;
                        DrawBezier(p0, p1, p2, p3, 20);
                    }
                }

                foreach (var (slotPos, direction) in connectors)
                {
                    var dotWorld = GetWorldPos(slotPos);
                    if (!dotWorld.HasValue) continue;

                    Gizmos.color = ColorDot;
                    Gizmos.DrawSphere(dotWorld.Value, DotRadius);

                    var targetCell = slotPos + direction;
                    var key        = MakeKey(slotPos, targetCell);

                    if (topology.ConnectedEdges.Contains(key))
                    {
                        if (!IsLowerSide(slotPos, targetCell)) continue;

                        var targetWorld = GetWorldPos(targetCell);
                        if (!targetWorld.HasValue) continue;

                        var ab = (targetWorld.Value - dotWorld.Value).normalized * DotRadius;
                        Gizmos.color = ColorConnected;
                        Gizmos.DrawLine(dotWorld.Value + ab, targetWorld.Value - ab);
                    }
                    else
                    {
                        Gizmos.color = ColorUnconnected;
                        var worldDir   = (direction.x * worldRight + direction.y * worldDown).normalized;
                        var arrowStart = dotWorld.Value + worldDir * DotRadius;
                        var arrowVec   = worldDir * cellSize * ArrowLength;
                        GizmosExtensions.DrawArrow2D(arrowStart, arrowVec, DotRadius * 2f);
                    }
                }
            }
        }

        private static void DrawBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments)
        {
            var prev = p0;
            for (var i = 1; i <= segments; i++)
            {
                var t    = i / (float)segments;
                var inv  = 1f - t;
                var next = inv * inv * inv * p0
                         + 3f * inv * inv * t * p1
                         + 3f * inv * t  * t * p2
                         + t  * t  * t      * p3;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private Vector2? GetWorldPos(Vector2Int gridPos)
        {
            if (_container == null || _slots == null) return null;

            var index = gridPos.x + gridPos.y * _container.GridSize.x;
            if (index < 0 || index >= _slots.Count) return null;

            var rt = _slots[index].RectTransform;
            return rt != null ? (Vector2)rt.position : (Vector2?)null;
        }

        private static (Vector2Int, Vector2Int) MakeKey(Vector2Int a, Vector2Int b) =>
            IsLowerSide(a, b) ? (a, b) : (b, a);

        private static bool IsLowerSide(Vector2Int a, Vector2Int b) =>
            a.y < b.y || (a.y == b.y && a.x < b.x);
    }
}