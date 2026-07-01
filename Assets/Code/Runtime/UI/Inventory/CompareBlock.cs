using System;
using System.Collections.Generic;
using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>drag-to-compare</b> body (tooltip-redesign spec 2026-06-30, "Interaction", slice 8):
    /// while the player holds an item over an <em>occupied</em> slot, this builds the held-item-vs-slot-item
    /// standalone read, aligned stat-by-stat so the presenter can print <c>dmg 8 vs 12 · rate 1.0s vs 0.4s</c>.
    ///
    /// Pure and fully unit-testable: each item is reduced to an ordered list of <b>keyed standalone stats</b>
    /// (its own read, not a chain diff — the compare is item-vs-item), then the two lists are aligned by key.
    /// <list type="bullet">
    ///   <item><b>Matched</b> — both items carry the key (two weapons' <c>dmg</c>, two amps' <c>Damage</c>):
    ///   one <see cref="CompareRow"/> with both sides filled.</item>
    ///   <item><b>Mismatched types</b> — a key only one item carries (a weapon's <c>dmg</c> vs an amplifier's
    ///   <c>Damage</c> share no keys): a row with the absent side <c>null</c>, so the presenter renders a
    ///   dash. A weapon's absolute <c>dmg</c> and an amplifier's <c>+</c> delta deliberately do <em>not</em>
    ///   align (different keys) — comparing an absolute to a delta would misinform.</item>
    /// </list>
    ///
    /// Values are pre-formatted semantic strings (each stat's own display format); color/emphasis and the
    /// literal "vs" separator stay the presenter's job. Chain-impact compare (how a swap changes the whole
    /// chain) is deferred (spec Gaps) — this is standalone item-vs-item only.
    /// </summary>
    public static class CompareBlock
    {
        public static CompareView Build(ITetrisItem held, ITetrisItem slotItem)
        {
            var rows = Align(Stats(held), Stats(slotItem));
            return new CompareView(held?.Name ?? string.Empty, slotItem?.Name ?? string.Empty, rows);
        }

        /// <summary>
        /// An item's <b>keyed standalone stats</b>, in display order. The key is what two items align on;
        /// the value is the pre-formatted read. Weapons read their base attack (absolute dmg / rate / cost);
        /// attachments read their own identity modifier (the same content the standalone tooltip shows), keyed
        /// by the stat/axis they touch so two same-kind pieces line up. A null item contributes nothing.
        /// </summary>
        private static IReadOnlyList<(string Key, string Value)> Stats(ITetrisItem item)
        {
            switch (item)
            {
                case IWeaponItem w:
                    return new[]
                    {
                        ("dmg",  $"{(float)w.Damage:F1}"),
                        ("rate", Interval((float)w.AttackSpeed)),
                        ("cost", $"{(float)w.ResourceCost:F1} [{w.CostResource}]"),
                    };

                case IAmplifierItem amp:
                    return new[] { ($"{amp.outputMod.stat}", $"{amp.outputMod.modifier}") };

                case IShifterItem sh:
                    return new[]
                    {
                        ($"{sh.inputMod.stat}",  $"{sh.inputMod.modifier}"),
                        ($"{sh.outputMod.stat}", $"{sh.outputMod.modifier}"),
                    };

                case IReactorItem r:
                {
                    var list = new List<(string, string)>
                    {
                        ("fires", PositionalDelta.FiringCondition(r.ReactorType)),
                    };
                    // Additive: the numeric input line appears only when the modifier is a real change
                    // (same no-op gate the piece/two-state views use), so a bare reactor compares clean.
                    if (PositionalDelta.IsMeaningful(r.inputMod.modifier))
                        list.Add(($"{r.inputMod.stat}", $"{r.inputMod.modifier}"));
                    return list;
                }

                case IConverterItem c:
                    return new[] { ($"{c.Axis}", $"→ {PositionalDelta.ConverterTarget(c)}") };

                default:
                    return Array.Empty<(string, string)>();
            }
        }

        // Union the two keyed lists preserving the held item's order first, then any slot-only keys. A key
        // present on both sides is one matched row; a key on only one side leaves the other side null (the
        // presenter dashes it). Matching consumes the slot entry positionally so a repeated key (e.g. a
        // shifter touching one stat on both its input and output) still pairs up one-for-one.
        private static IReadOnlyList<CompareRow> Align(
            IReadOnlyList<(string Key, string Value)> held,
            IReadOnlyList<(string Key, string Value)> slot)
        {
            var rows          = new List<CompareRow>();
            var slotRemaining = new List<(string Key, string Value)>(slot);

            foreach (var (key, value) in held)
            {
                var idx = slotRemaining.FindIndex(s => s.Key == key);
                if (idx >= 0)
                {
                    rows.Add(new CompareRow(key, value, slotRemaining[idx].Value));
                    slotRemaining.RemoveAt(idx);
                }
                else
                    rows.Add(new CompareRow(key, value, null));
            }

            foreach (var (key, value) in slotRemaining)
                rows.Add(new CompareRow(key, null, value));

            return rows;
        }

        // Attack interval (1/speed) in seconds — the player-facing rate read (a shorter interval is faster),
        // mirroring the standalone weapon tooltip's Interval so the compare and the full tooltip agree.
        private static string Interval(float attackSpeed) =>
            attackSpeed > 0f ? $"{1f / attackSpeed:0.00}s" : "—";
    }

    /// <summary>
    /// One aligned stat in a drag-compare: its <see cref="Label"/> (the key the two items shared) and the
    /// held- and slot-side pre-formatted values. A side is <c>null</c> when that item doesn't carry the stat
    /// (a mismatched-type row); the presenter renders the missing side as a dash. Keeping raw held/slot
    /// strings (rather than a joined "a vs b") is what makes the alignment unit-testable.
    /// </summary>
    public readonly struct CompareRow
    {
        public string Label { get; }
        public string Held  { get; }
        public string Slot  { get; }

        public CompareRow(string label, string held, string slot)
        {
            Label = label;
            Held  = held;
            Slot  = slot;
        }
    }

    /// <summary>The held-vs-slot compare: the two item names and the aligned <see cref="Rows"/>.</summary>
    public readonly struct CompareView
    {
        public string                   HeldName { get; }
        public string                   SlotName { get; }
        public IReadOnlyList<CompareRow> Rows     { get; }

        public CompareView(string heldName, string slotName, IReadOnlyList<CompareRow> rows)
        {
            HeldName = heldName;
            SlotName = slotName;
            Rows     = rows;
        }
    }
}
