using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;

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

        /// <summary>
        /// The <b>axis-change</b> lines of a piece's marginal delta: the categorical (non-numeric) shifts a
        /// piece makes to the weapon's Delivery / Affinity / Anchor axes and its cost <em>pool</em>. A
        /// <see cref="IConverterItem"/> is the usual source (it reclassifies one axis — kind, not amount),
        /// read from the piece's before/with snapshots rather than the item, so it stays a chain-positional
        /// delta. <b>Additive</b>: a line appears only for an axis this piece actually changes.
        ///
        /// <paramref name="detailed"/> is the Alt expansion (spec §3 Converter row): off, each line names
        /// only the <em>result</em> (<c>→ Aoe</c> — "converts to"); on, it shows the full <em>from → to</em>
        /// (<c>Single → Aoe</c>). Color stays the presenter's job (direction only) — these are the semantic
        /// strings, uncolored, so the axis logic is unit-testable without driving Unity.
        /// </summary>
        public static IReadOnlyList<string> AxisDeltas(PieceDelta piece, bool detailed)
        {
            var parts = new List<string>();
            AddAxis(parts, piece.Before.Delivery, piece.With.Delivery, detailed);
            AddAxis(parts, piece.Before.Affinity, piece.With.Affinity, detailed);
            AddAxis(parts, piece.Before.Anchor,   piece.With.Anchor,   detailed);
            AddPool(parts, piece.Before.CostResource, piece.With.CostResource, detailed);
            return parts;
        }

        // One reclassified axis: "→ To" (result only), or with Alt the whole move "From → To".
        private static void AddAxis<T>(ICollection<string> parts, T before, T with, bool detailed)
            where T : struct, Enum
        {
            if (EqualityComparer<T>.Default.Equals(before, with)) return;
            parts.Add(detailed ? $"{before} → {with}" : $"→ {with}");
        }

        // The cost pool keeps its "pool" lead so a resource swap doesn't read like an axis conversion.
        private static void AddPool(ICollection<string> parts, ResourceType before, ResourceType with, bool detailed)
        {
            if (before == with) return;
            parts.Add(detailed ? $"pool {before} → {with}" : $"pool → {with}");
        }

        /// <summary>
        /// The per-attachment <b>active-delta content</b> (tooltip-redesign spec §3, slice 4): the §3
        /// table's "active delta (no Alt)" column, read <em>intrinsically</em> from the item's own
        /// modifiers/axis — not from a chain diff. This is the "what does this piece do?" answer for an
        /// attachment's <em>own</em> hover:
        /// <list type="bullet">
        ///   <item><b>Amplifier</b> — its output modifier, e.g. <c>Damage +6</c>.</item>
        ///   <item><b>Reactor</b> — the firing condition (<c>fires when hit</c>) plus its input modifier,
        ///   e.g. <c>ManaCost * 120 %</c>.</item>
        ///   <item><b>Shifter</b> — the input↔output economy trade.</item>
        ///   <item><b>Converter</b> — the target it converts <em>to</em>, e.g. <c>→ Aoe</c> (the <em>from</em>
        ///   side is an Alt/later-slice concern).</item>
        /// </list>
        /// <b>Additive</b>: a numeric line appears only when its modifier is non-default, so future fields
        /// don't force layout churn (spec §3 note). Non-attachments return an empty list. The Alt "before →
        /// after" equation expansion is a later slice; this is the active (no-Alt) content only.
        /// </summary>
        public static IReadOnlyList<string> Describe(ITetrisItem item)
        {
            switch (item)
            {
                case IAmplifierItem amp:
                    return ModLine(amp.outputMod.stat, amp.outputMod.modifier);

                case IShifterItem sh:
                    // The economy trade is one semantic move — the shifter's identity, always shown.
                    return new[]
                    {
                        $"{sh.inputMod.stat} {sh.inputMod.modifier} ↔ {sh.outputMod.stat} {sh.outputMod.modifier}",
                    };

                case IReactorItem reactor:
                    var lines = new List<string> { $"fires {FiringCondition(reactor.ReactorType)}" };
                    if (IsMeaningful(reactor.inputMod.modifier))
                        lines.Add($"{reactor.inputMod.stat} {reactor.inputMod.modifier}");
                    return lines;

                case IConverterItem converter:
                    return new[] { $"→ {ConverterTarget(converter)}" };

                default:
                    return Array.Empty<string>();
            }
        }

        /// <summary>Player-facing firing-condition phrase for a reactor's trigger event. Shared by the
        /// attachment view, the weapon's terminal rate line, and the piece list so there is one map.</summary>
        public static string FiringCondition(ReactorType type) => type switch
        {
            ReactorType.OnSelfHit         => "when hit",
            ReactorType.OnManaDeplete     => "when mana empties",
            ReactorType.OnEnemyDeath      => "when an enemy dies",
            ReactorType.OnAllyAttacks     => "when an ally attacks",
            ReactorType.OnAllyKills       => "when an ally kills",
            ReactorType.OnNearbyEnemyDies => "when a nearby enemy dies",
            _                             => type.ToString(),
        };

        // The kind a Converter reclassifies its axis to (ADR-0004 §1) — the "to" side only.
        private static string ConverterTarget(IConverterItem c) => c.Axis switch
        {
            ConverterAxis.Delivery => c.ToDelivery.ToString(),
            ConverterAxis.Affinity => c.ToAffinity.ToString(),
            ConverterAxis.Anchor   => c.ToAnchor.ToString(),
            ConverterAxis.Resource => c.ToResource.ToString(),
            _                      => c.Axis.ToString(),
        };

        // Amp/shifter-style "stat modifier" line, dropped when the modifier is a no-op (additive rule).
        private static IReadOnlyList<string> ModLine<T>(T stat, Modifier mod) where T : Enum =>
            IsMeaningful(mod) ? new[] { $"{stat} {mod}" } : Array.Empty<string>();

        private const float Epsilon = 1e-4f;

        // A flat/percent-add modifier of ~0 changes nothing → not worth a line. Percent-mult / overwrite
        // are deliberate authored values (× x %, = x), so they always print.
        private static bool IsMeaningful(Modifier mod) => mod.Type switch
        {
            ModifierType.FlatAdd or ModifierType.PercentAdd => Math.Abs((float)mod) > Epsilon,
            _                                               => true,
        };

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
