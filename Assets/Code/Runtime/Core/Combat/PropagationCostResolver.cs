using System;
using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// A node in an attack's cost tree (ADR-0004 §4 / ADR-0006): the root weapon's children are payload
    /// deliveries, each carrying a single cost <see cref="Modifier"/> (value + <c>ModifierType</c>) and
    /// its own ordered children. A linear chain is nested single-children (propagation order); a Splitter
    /// fork is a node with more than one child (Splitter content is deferred, but the topology is ready).
    /// The node owns no pool — only its marginal contribution to the one shared cost pool.
    /// </summary>
    public sealed class CostNode
    {
        public Modifier Cost { get; }
        public IReadOnlyList<CostNode> Children { get; }

        public CostNode(Modifier cost, IReadOnlyList<CostNode> children = null)
        {
            Cost = cost;
            Children = children ?? Array.Empty<CostNode>();
        }
    }

    /// <summary>Which nodes detonated and how much the whole attack drained from the pool.</summary>
    public readonly struct PropagationResult
    {
        /// <summary>False when the pool couldn't even cover the weapon's effective base — nothing fires.</summary>
        public bool RootFired { get; }

        /// <summary>The payload nodes that fired (the root weapon is implied by <see cref="RootFired"/>).</summary>
        public IReadOnlyCollection<CostNode> FiredNodes { get; }

        /// <summary>Effective base plus every fired node's marginal cost — what left the pool.</summary>
        public float TotalSpent { get; }

        public PropagationResult(bool rootFired, IReadOnlyCollection<CostNode> firedNodes, float totalSpent)
        {
            RootFired  = rootFired;
            FiredNodes = firedNodes;
            TotalSpent = totalSpent;
        }
    }

    /// <summary>
    /// Pure fail-forward propagation-cost walker (ADR-0006). Given the weapon's base cost, its Reactor
    /// cost modifiers, the payload cost tree, and a pool balance, it decides which nodes detonate by
    /// paying as it walks depth-first. The cost math is delegated to <see cref="MutableFloat"/> (Decision
    /// 5 — <em>use</em> the stat pipeline, don't mirror it), so <c>PercentMult</c>'s "deeper costs more"
    /// compounding falls out for free. No pawns, registry, grid, or engine — unit-testable in isolation,
    /// like <see cref="DeliveryResolver"/>.
    ///
    /// Rules: the Reactor mods applied to the base give the <b>effective base</b> = the root gate
    /// (Decision 6); if the pool can't cover it, nothing fires. Then each node's <b>marginal</b> (the
    /// delta it adds to the running cost) is paid from the pool — affordable → fire and recurse;
    /// unaffordable → prune the node and its whole subtree, spending nothing (Decision 2, fail-forward).
    /// A Splitter fork funds siblings <b>highest-subtree-drain first</b> off the one shared pool, each
    /// sibling restarting from the fork's running cost so they never compound with each other (Decision 3).
    /// </summary>
    public static class PropagationCostResolver
    {
        private const float Epsilon = 1e-4f;

        public static PropagationResult Resolve(
            float weaponBaseCost,
            IReadOnlyList<Modifier> reactorMods,
            IReadOnlyList<CostNode> payloadTree,
            float poolBalance)
        {
            var cost = new MutableFloat(weaponBaseCost);
            if (reactorMods != null)
                foreach (var mod in reactorMods)
                    cost.AddModifier(mod);

            var effectiveBase = (float)cost;
            var fired = new HashSet<CostNode>();

            // Root gate (Decision 6): the weapon cannot fire — and so nothing downstream can — without
            // the (Reactor-scaled) effective base in the pool.
            if (!Affordable(effectiveBase, poolBalance))
                return new PropagationResult(false, fired, 0f);

            var pool = poolBalance - effectiveBase;
            if (payloadTree != null)
                WalkChildren(payloadTree, cost, ref pool, fired, added: null);

            return new PropagationResult(true, fired, poolBalance - pool);
        }

        /// <summary>
        /// A single child is the linear propagation-order case (Decision 2): walk it in place, threading
        /// the parent's undo log so descendants stay tracked. More than one child is a Splitter fork
        /// (Decision 3): order by whole-subtree drain (descending, stable by original index) and isolate
        /// each sibling — track what its subtree adds to the running cost and undo it before the next
        /// sibling, so siblings compete only for the shared pool, never inflating one another.
        /// </summary>
        private static void WalkChildren(IReadOnlyList<CostNode> children, MutableFloat cost, ref float pool,
            HashSet<CostNode> fired, List<Modifier> added)
        {
            if (children.Count == 0)
                return;

            if (children.Count == 1)
            {
                WalkNode(children[0], cost, ref pool, fired, added);
                return;
            }

            var ordered = children
                .Select((node, index) => (node, index, drain: SubtreeDrain(node, cost)))
                .OrderByDescending(x => x.drain)
                .ThenBy(x => x.index)
                .Select(x => x.node)
                .ToList();

            foreach (var sibling in ordered)
            {
                var siblingAdded = new List<Modifier>();
                WalkNode(sibling, cost, ref pool, fired, siblingAdded);
                for (var i = siblingAdded.Count; i-- > 0;)
                    cost.TryRemoveModifier(siblingAdded[i]);
            }
        }

        private static void WalkNode(CostNode node, MutableFloat cost, ref float pool,
            HashSet<CostNode> fired, List<Modifier> added)
        {
            var before = (float)cost;
            cost.AddModifier(node.Cost);
            var marginal = (float)cost - before;

            if (!Affordable(marginal, pool))
            {
                // Prune (Decision 2): undo this node's cost, spend nothing, and don't recurse — its
                // descendants cannot exist without it.
                cost.TryRemoveModifier(node.Cost);
                return;
            }

            pool -= marginal;
            fired.Add(node);
            added?.Add(node.Cost);
            WalkChildren(node.Children, cost, ref pool, fired, added);
        }

        /// <summary>
        /// The fork ordering key (Decision 3): how much this node's <em>whole subtree</em> would add to
        /// the current running cost if every node in it fired. Computed non-destructively against the
        /// live <paramref name="cost"/> — add the whole subtree, read the delta, then remove it — so it
        /// reflects the fork's running cost R (a downstream PercentMult inflates the drain faithfully).
        /// </summary>
        private static float SubtreeDrain(CostNode node, MutableFloat cost)
        {
            var before = (float)cost;
            var probe = new List<Modifier>();
            AddSubtree(node, cost, probe);
            var drain = (float)cost - before;
            for (var i = probe.Count; i-- > 0;)
                cost.TryRemoveModifier(probe[i]);
            return drain;
        }

        private static void AddSubtree(CostNode node, MutableFloat cost, List<Modifier> probe)
        {
            cost.AddModifier(node.Cost);
            probe.Add(node.Cost);
            foreach (var child in node.Children)
                AddSubtree(child, cost, probe);
        }

        private static bool Affordable(float cost, float pool) => cost <= pool + Epsilon;
    }
}
