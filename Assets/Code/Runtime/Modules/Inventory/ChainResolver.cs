using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using UnityEngine;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class ChainTopology
    {
        public IReadOnlyList<IItemChain>                                           Chains               { get; }
        public HashSet<(Vector2Int, Vector2Int)>                                   ConnectedEdges       { get; }
        public IReadOnlyDictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>> DownstreamConnectors { get; }
        public IReadOnlyDictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>> UpstreamConnectors   { get; }
        /// <summary>Items resolved as chain roots by first-pass BFS position, not interface marker.</summary>
        public IReadOnlyCollection<ITetrisItem>                                    Roots                { get; }

        public ChainTopology(
            IReadOnlyList<IItemChain>                                        chains,
            HashSet<(Vector2Int, Vector2Int)>                                connectedEdges,
            Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>       downstreamConnectors,
            Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>       upstreamConnectors,
            HashSet<ITetrisItem>                                             roots)
        {
            Chains               = chains;
            ConnectedEdges       = connectedEdges;
            DownstreamConnectors = downstreamConnectors;
            UpstreamConnectors   = upstreamConnectors;
            Roots                = roots;
        }

        public static readonly ChainTopology Empty = new(
            System.Array.Empty<IItemChain>(),
            new HashSet<(Vector2Int, Vector2Int)>(),
            new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>(),
            new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>(),
            new HashSet<ITetrisItem>());
    }

    public static class ChainResolver
    {
        public static IReadOnlyList<IItemChain> Resolve(ITetrisContainer container)
            => ResolveTopology(container).Chains;

        public static ChainTopology ResolveTopology(ITetrisContainer container)
        {
            var adjacency  = BuildAdjacency(container);
            var positionOf = container.Contents.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            var chains               = new List<IItemChain>();
            var connectedEdges       = new HashSet<(Vector2Int, Vector2Int)>();
            var downstreamConnectors = new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>();
            var upstreamConnectors   = new Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>>();
            var roots                = new HashSet<ITetrisItem>();

            // Weapon-centric: every weapon owns its firing sources. The reactors connected to it are
            // the sources (each fires on its own event); with none, the weapon fires itself on its
            // timer. One firing per source — never one per connector — so a weapon amplified on both
            // sides is a single firing carrying both amps (the double-fire is gone at the source).
            foreach (var weapon in container.Contents.Values.OfType<IWeaponItem>())
            {
                var sources = FiringSources(weapon, adjacency);
                if (sources.Count == 0)
                    sources.Add(weapon); // no reactor → timer-driven, the weapon is its own source

                foreach (var source in sources)
                {
                    var modifiers = GatherModifiers(source, weapon, positionOf[source], adjacency, container,
                        connectedEdges, upstreamConnectors, downstreamConnectors);

                    // A reactor source must reach this weapon; defensive only — FiringSources found it
                    // by walking the trigger graph back from the weapon, so the path always exists.
                    if (source is not IWeaponItem && !modifiers.Exists(m => m is IWeaponItem))
                    {
                        Debug.LogWarning($"[ChainResolver] Firing source '{source.Name}' reached no weapon — skipped.");
                        continue;
                    }

                    roots.Add(source);
                    chains.Add(new ItemChain(source, modifiers));
                    //LogChain(chains[^1]);
                }
            }

            if (chains.Count == 0)
                return ChainTopology.Empty;

            return new ChainTopology(chains, connectedEdges, downstreamConnectors, upstreamConnectors, roots);
        }

        // ── Firing sources ────────────────────────────────────────────────

        /// <summary>
        /// The reactors that drive this weapon, found by walking the trigger graph (shifters and
        /// reactors) outward from the weapon. Only reactors are firing sources — shifters are
        /// stat-shaping walls. Deduplicated by <see cref="ReactorType"/>: two reactors on the same
        /// event collapse to one firing (the weapon's trigger list holds unique events).
        /// </summary>
        private static List<ITetrisItem> FiringSources(
            IWeaponItem                                weapon,
            Dictionary<ITetrisItem, List<ITetrisItem>> adjacency)
        {
            var sources    = new List<ITetrisItem>();
            var seenEvents = new HashSet<ReactorType>();
            var visited    = new HashSet<ITetrisItem> { weapon };
            var queue      = new Queue<ITetrisItem>();

            if (adjacency.TryGetValue(weapon, out var weaponNeighbours))
                foreach (var neighbour in weaponNeighbours)
                    if (IsTrigger(neighbour))
                        queue.Enqueue(neighbour);

            while (queue.Count > 0)
            {
                var trigger = queue.Dequeue();
                if (!visited.Add(trigger)) continue;

                if (trigger is IReactorItem reactor && seenEvents.Add(reactor.ReactorType))
                    sources.Add(reactor);

                if (adjacency.TryGetValue(trigger, out var neighbours))
                    foreach (var neighbour in neighbours)
                        if (IsTrigger(neighbour) && !visited.Contains(neighbour))
                            queue.Enqueue(neighbour);
            }

            return sources;
        }

        /// <summary>
        /// Gathers a firing's contributors by BFS from its <paramref name="source"/>. Two walls keep
        /// firings scoped: a different weapon ends the branch (it is its own firing), and a trigger
        /// cannot be entered from a non-trigger (existing rule) so each reactor claims only its own
        /// side's shifters. The first hop skips the trigger-wall so the source reaches its immediate
        /// shifter. Connectors/edges are recorded here for the overlay.
        /// </summary>
        private static List<ITetrisItem> GatherModifiers(
            ITetrisItem                                                source,
            IWeaponItem                                                weapon,
            Vector2Int                                                 sourcePos,
            Dictionary<ITetrisItem, List<ITetrisItem>>                 adjacency,
            ITetrisContainer                                           container,
            HashSet<(Vector2Int, Vector2Int)>                          connectedEdges,
            Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>> upstreamConnectors,
            Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>> downstreamConnectors)
        {
            var modifiers = new List<ITetrisItem>();
            var visited   = new HashSet<ITetrisItem> { source };
            var queue     = new Queue<(ITetrisItem item, Vector2Int pos, Vector2Int inSlotPos, Vector2Int inDirection)>();

            foreach (var (slotPos, direction) in source.GetGridConnectors(sourcePos))
            {
                if (!TryGetValidNeighbour(adjacency, container, source, slotPos, direction,
                        out var firstNeighbour, out var firstOrigin)) continue;
                if (firstNeighbour is IWeaponItem && firstNeighbour != weapon) continue;

                connectedEdges.Add(MakeKey(slotPos, slotPos + direction));
                MarkConnector(upstreamConnectors, source, slotPos, direction);
                queue.Enqueue((firstNeighbour, firstOrigin, slotPos + direction, -direction));
            }

            while (queue.Count > 0)
            {
                var (current, currentPos, inSlotPos, inDirection) = queue.Dequeue();
                if (!visited.Add(current)) continue;
                modifiers.Add(current);

                MarkConnector(downstreamConnectors, current, inSlotPos, inDirection);

                foreach (var (slotPos, direction) in current.GetGridConnectors(currentPos))
                {
                    if (!TryGetValidNeighbour(adjacency, container, current, slotPos, direction,
                            out var next, out var nextOrigin)) continue;
                    if (visited.Contains(next)) continue;
                    if (next is IWeaponItem && next != weapon) continue;     // other weapon = its own firing
                    if (IsTrigger(next) && !IsTrigger(current)) continue;    // trigger wall

                    connectedEdges.Add(MakeKey(slotPos, slotPos + direction));
                    MarkConnector(upstreamConnectors, current, slotPos, direction);
                    queue.Enqueue((next, nextOrigin, slotPos + direction, -direction));
                }
            }

            return modifiers;
        }

        /// <summary>
        /// Builds an undirected connection graph filtered by connection validity rules.
        /// Only valid bidirectional connector pairs produce edges.
        /// </summary>
        private static Dictionary<ITetrisItem, List<ITetrisItem>> BuildAdjacency(ITetrisContainer container)
        {
            var adj = new Dictionary<ITetrisItem, List<ITetrisItem>>();

            foreach (var item in container.Contents.Values)
                adj[item] = new List<ITetrisItem>();

            foreach (var (pos, item) in container.Contents)
            {
                foreach (var (slotPos, direction) in item.GetGridConnectors(pos))
                {
                    var targetCell = slotPos + direction;
                    if (!container.ContentPointer.TryGetValue(targetCell, out var neighborOrigin)) continue;
                    if (!container.Contents.TryGetValue(neighborOrigin, out var neighbor)) continue;
                    if (!HasMatchingConnector(neighbor, neighborOrigin, targetCell, -direction)) continue;
                    if (!IsValidConnection(item, neighbor)) continue;
                    if (adj[item].Contains(neighbor)) continue;

                    adj[item].Add(neighbor);
                    adj[neighbor].Add(item);
                }
            }

            return adj;
        }
        
        // ── Logging ───────────────────────────────────────────────────────

        public static void LogChain(IItemChain chain)
        {
            if (!chain.IsValid) return;
            Debug.Log(FormatDetailed(chain));
        }
        
        private static string FormatDetailed(IItemChain chain)
        {
            var weapon = chain.Weapon;
            var sb     = new System.Text.StringBuilder("[Chain]");
            sb.Append($" {GetSemanticLabel(chain.Root, true)}({chain.Root.Name})");
            if (weapon != null)
                sb.Append($" dmg:{(float)weapon.Damage:F1} spd:{(float)weapon.AttackSpeed:F1} cost:{(float)weapon.ResourceCost:F1}");
            foreach (var item in chain.Modifiers)
            {
                var isPayload = item is IWeaponItem w && w != weapon;
                sb.Append($" → {GetSemanticLabel(item, false, isPayload)}({item.Name}");
                if (isPayload)
                    sb.Append($"|{((IWeaponItem)item).Payload.Condition}");
                sb.Append(")");
            }
            return sb.ToString();
        }

        private static string GetSemanticLabel(ITetrisItem item, bool isRoot, bool isPayload = false) => item switch
        {
            IWeaponItem    when isRoot    => "Weapon",
            IWeaponItem    when isPayload => "Payload",
            IWeaponItem                   => "Weapon",
            IAmplifierItem             => "Amplifier",
            IConverterItem             => "Converter",
            _              when isRoot => "Trigger",
            _                          => item.GetType().Name,
        };
        
        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if slotPos+direction points to a neighbour that is both
        /// connector-matched and present in the validated adjacency graph.
        /// Single check point used by both root→first and BFS inner loop.
        /// </summary>
        private static bool TryGetValidNeighbour(
            Dictionary<ITetrisItem, List<ITetrisItem>> adjacency,
            ITetrisContainer                           container,
            ITetrisItem                                from,
            Vector2Int                                 slotPos,
            Vector2Int                                 direction,
            out ITetrisItem                            neighbour,
            out Vector2Int                             neighbourOrigin)
        {
            neighbour       = null;
            neighbourOrigin = default;

            var targetCell = slotPos + direction;
            if (!container.ContentPointer.TryGetValue(targetCell, out neighbourOrigin)) return false;
            if (!container.Contents.TryGetValue(neighbourOrigin, out neighbour)) return false;
            if (!HasMatchingConnector(neighbour, neighbourOrigin, targetCell, -direction)) return false;
            if (!adjacency.TryGetValue(from, out var validNeighbors)) return false;
            return validNeighbors.Contains(neighbour);
        }

        private static bool IsValidConnection(ITetrisItem a, ITetrisItem b) => (a, b) switch
        {
            (IReactorItem,   IReactorItem)   => false,
            (IReactorItem,   IAmplifierItem) => false,
            (IReactorItem,   IConverterItem) => false,
            (IAmplifierItem, IReactorItem)   => false,
            (IConverterItem, IReactorItem)   => false,
            (IShifterItem, IAmplifierItem) => false,
            (IShifterItem, IConverterItem) => false,
            (IAmplifierItem, IShifterItem) => false,
            (IConverterItem, IShifterItem) => false,
            _                                => true,
        };

        private static bool IsTrigger(ITetrisItem item) => item is IShifterItem or IReactorItem;

        private static bool HasMatchingConnector(
            ITetrisItem item, Vector2Int placement,
            Vector2Int  expectedSlotPos, Vector2Int expectedDirection)
        {
            foreach (var (slotPos, direction) in item.GetGridConnectors(placement))
                if (slotPos == expectedSlotPos && direction == expectedDirection)
                    return true;
            return false;
        }

        private static void MarkConnector(
            Dictionary<ITetrisItem, HashSet<(Vector2Int, Vector2Int)>> dict,
            ITetrisItem item, Vector2Int slotPos, Vector2Int direction)
        {
            if (!dict.TryGetValue(item, out var set))
            {
                set = new HashSet<(Vector2Int, Vector2Int)>();
                dict[item] = set;
            }
            set.Add((slotPos, direction));
        }

        private static (Vector2Int, Vector2Int) MakeKey(Vector2Int a, Vector2Int b) =>
            IsLowerSide(a, b) ? (a, b) : (b, a);

        private static bool IsLowerSide(Vector2Int a, Vector2Int b) =>
            a.y < b.y || (a.y == b.y && a.x < b.x);
    }
}