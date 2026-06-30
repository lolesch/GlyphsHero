using System;
using System.Collections.Generic;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Maps a resolved weapon chain's payloads into the cost tree the <see cref="PropagationCostResolver"/>
    /// walks (ADR-0006 / ADR-0004 §4). Without a Splitter a chain is a single <b>linear lineage in
    /// propagation order</b>: the first payload is the root weapon's child, and each subsequent payload
    /// nests as that node's single child — so an upstream node the pool can't fund prunes everyone
    /// downstream (fail-forward). Pure and engine-free; the node→payload pairing lets the caller detonate
    /// exactly the payloads the walk funded, in order.
    /// </summary>
    public static class PayloadCostTree
    {
        /// <summary>
        /// Builds the linear lineage from already-extracted (cost, payload) pairs given in propagation
        /// order. Generic in the payload type so it is unit-testable without weapon fakes. Returns the
        /// lineage root for the walker plus the ordered node→payload pairs so the caller can fire the
        /// funded payloads deterministically.
        /// </summary>
        public static (IReadOnlyList<CostNode> roots, IReadOnlyList<(CostNode node, T payload)> ordered)
            BuildLineage<T>(IReadOnlyList<(Modifier cost, T payload)> payloads)
        {
            if (payloads == null || payloads.Count == 0)
                return (Array.Empty<CostNode>(), Array.Empty<(CostNode, T)>());

            var ordered = new (CostNode node, T payload)[payloads.Count];

            // Build bottom-up so each node nests the next as its single child; the first payload ends up
            // as the lineage root, which the walker pays first.
            CostNode child = null;
            for (var i = payloads.Count; i-- > 0;)
            {
                var (cost, payload) = payloads[i];
                var node = child == null ? new CostNode(cost) : new CostNode(cost, new[] { child });
                ordered[i] = (node, payload);
                child = node;
            }

            return (new[] { child }, ordered);
        }
    }
}
