using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>positional delta model</b> (tooltip-redesign spec 2026-06-30, slice 3): the pure,
    /// Unity-free logic behind "hover the weapon → see the whole chain; hover a piece → see that piece's
    /// marginal effect at its spot".
    ///
    /// Two readings, one rule:
    /// <list type="bullet">
    ///   <item><see cref="Totals"/> — the weapon is the <em>terminal readout</em>: the chain's final
    ///   resolved <see cref="WeaponStats"/> (not a delta).</item>
    ///   <item><see cref="Pieces"/> — one <see cref="PieceDelta"/> per contributing piece, in the
    ///   <see cref="WeaponStatResolver"/> apply order (root → modifiers). Each carries the resolved
    ///   snapshot <em>before</em> and <em>with</em> that piece, so the difference is exactly its marginal
    ///   contribution — the same before/after diff the old tooltip computed inline, factored out so it can
    ///   be unit-tested without driving Unity.</item>
    /// </list>
    ///
    /// Weapons are excluded from the piece list: the driving weapon <em>is</em> the terminal readout, and
    /// a downstream payload weapon isn't a stat contributor to the root (it carries its own child delivery,
    /// summarised separately). So the piece list is exactly the stat-shaping attachments.
    /// </summary>
    public static class PositionalDelta
    {
        /// <summary>The chain's final resolved totals — the weapon's terminal readout.</summary>
        public static WeaponStats Totals(IItemChain chain) => WeaponStatResolver.Resolve(chain);

        /// <summary>
        /// The ordered per-piece marginal deltas. Apply order = <see cref="OrderedItems"/> (root first,
        /// then modifiers) — the same order <see cref="WeaponStatResolver"/> folds contributors in, so a
        /// piece's "before" is the chain up to but excluding it and its "with" includes it. Weapons (the
        /// driving weapon and any payload weapon) are skipped: they are not stat contributors here.
        /// </summary>
        public static IReadOnlyList<PieceDelta> Pieces(IItemChain chain)
        {
            var result = new List<PieceDelta>();
            var weapon = chain.Weapon;
            if (weapon == null) return result;

            var ordered = OrderedItems(chain);
            for (var i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i];
                if (item is IWeaponItem) continue; // terminal readout / payload — never a piece-list delta

                var before = WeaponStatResolver.Resolve(weapon, ordered.Take(i));
                var with   = WeaponStatResolver.Resolve(weapon, ordered.Take(i + 1));
                result.Add(new PieceDelta(item, before, with));
            }

            return result;
        }

        // Root then modifiers — the ChainResolver order WeaponStatResolver folds contributors in.
        private static List<ITetrisItem> OrderedItems(IItemChain chain)
        {
            var list = new List<ITetrisItem>();
            if (chain.Root != null) list.Add(chain.Root);
            list.AddRange(chain.Modifiers);
            return list;
        }
    }

    /// <summary>
    /// One contributing piece's marginal effect at its position in the chain: the resolved
    /// <see cref="WeaponStats"/> snapshot <see cref="Before"/> it applies and <see cref="With"/> it applied.
    /// The presenter (the tooltip) turns the field-by-field difference into a directional, coloured line;
    /// keeping the raw snapshots here (rather than a formatted string) is what makes the model testable.
    /// </summary>
    public readonly struct PieceDelta
    {
        public ITetrisItem Item   { get; }
        public WeaponStats Before { get; }
        public WeaponStats With   { get; }

        public PieceDelta(ITetrisItem item, WeaponStats before, WeaponStats with)
        {
            Item   = item;
            Before = before;
            With   = with;
        }
    }
}
