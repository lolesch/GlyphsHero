using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's Alt-only <b>breadcrumb</b> (tooltip-redesign spec 2026-06-30, §4, slice 6): a single
    /// horizontal path of a chain's items in <em>real connection order</em> — <c>Root → … → Weapon</c> —
    /// with the hovered item bracketed:
    /// <code>Reactor → Amp → [Iron Amp] → Crossblades</code>
    ///
    /// This replaces the deleted <c>BuildChainSentence</c>, whose <c>↓</c> diagram walked the chain
    /// <em>outward from the hovered item</em> and so read <b>opposite</b> to the grid connections (it drew
    /// the path backwards). The order here is exactly <see cref="OrderedItems"/> — the same root-first apply
    /// order <see cref="WeaponStatResolver"/> folds contributors in, and the order the piece list uses — so
    /// the breadcrumb, the math, and the grid all read the same direction.
    ///
    /// Pure and Unity-free (the presenter only decides when to show it — Alt held). Branch rendering is not
    /// implemented yet; when chains branch, a branch point is denoted by <see cref="Branch"/> rather than
    /// drawing side-paths.
    /// </summary>
    public static class Breadcrumb
    {
        /// <summary>Reserved glyph for a future branch point in the path (branches aren't implemented yet;
        /// spec §4). Kept here so the reservation lives with the renderer that will use it.</summary>
        public const string Branch = "⑂";

        private const string Separator = " → ";

        /// <summary>
        /// The breadcrumb for the chain the <paramref name="item"/> belongs to within
        /// <paramref name="topology"/>. Empty when the item sits in no chain (loose / standalone) — a
        /// standalone item has no path to draw.
        /// </summary>
        public static string Build(ChainTopology topology, ITetrisItem item)
        {
            if (topology == null || item == null) return string.Empty;

            var chain = topology.Chains
                .FirstOrDefault(c => c.Root == item || c.Modifiers.Contains(item));
            return Build(chain, item);
        }

        /// <summary>
        /// The breadcrumb for one <paramref name="chain"/>: its items in connection order (root → weapon),
        /// the <paramref name="item"/> bracketed. Empty for a null/empty chain.
        /// </summary>
        public static string Build(IItemChain chain, ITetrisItem item)
        {
            if (chain == null) return string.Empty;

            var ordered = OrderedItems(chain);
            if (ordered.Count == 0) return string.Empty;

            return string.Join(Separator,
                ordered.Select(i => i == item ? $"[{i.Name}]" : i.Name));
        }

        // Root then modifiers — the ChainResolver connection order (root → weapon), NOT outward from the
        // hovered item. This direction is the whole point of the breadcrumb replacing BuildChainSentence.
        private static List<ITetrisItem> OrderedItems(IItemChain chain)
        {
            var list = new List<ITetrisItem>();
            if (chain.Root != null) list.Add(chain.Root);
            list.AddRange(chain.Modifiers);
            return list;
        }
    }
}
