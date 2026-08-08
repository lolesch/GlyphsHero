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
        /// Which of the weapon's terminal stats (issue #19) the chain actually touched — one flag per
        /// numeric <see cref="WeaponStats"/> field the weapon-totals line renders. A stat counts as
        /// changed when the resolved total differs from the weapon's own base (zero contributors) by
        /// more than the additive no-op <see cref="Epsilon"/>, so the totals renderer can skip a line
        /// entirely rather than repeat a value nothing in the chain moved.
        /// </summary>
        public static TerminalStats ChangedStats(IItemChain chain)
        {
            var weapon = chain.Weapon;
            if (weapon == null) return default;

            var totals = Totals(chain);
            return new TerminalStats(
                Math.Abs((float)weapon.Damage - totals.Damage) > Epsilon,
                Math.Abs((float)weapon.AttackSpeed - totals.AttackSpeed) > Epsilon,
                Math.Abs((float)weapon.ResourceCost - totals.ResourceCost) > Epsilon);
        }

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
        /// <paramref name="detailed"/> is the Details expansion (spec §3 Converter row): off, each line names
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

        // One reclassified axis: "→ To" (result only), or with Details mode the whole move "From → To".
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
        /// A reactor's own <b>input</b> equation (spec §3 Reactor row): the modifier alone
        /// (<c>ManaCost * 120 %</c>) or, under Details mode, the full <c>[base X] modifier = result</c> equation —
        /// the base/result read off whichever <see cref="WeaponStats"/> field <c>inputMod.stat</c>
        /// targets, from the piece's own before/with snapshot (so it reflects this reactor's marginal
        /// contribution, not the whole chain). <c>ProcChance</c> has no backing <see cref="WeaponStats"/>
        /// field (<see cref="WeaponStatResolver"/> drops it silently), so it falls back to the modifier
        /// alone even under Details mode. Empty when the modifier is a no-op (same additive threshold as
        /// <see cref="Describe"/>), so the caller can skip the line entirely.
        /// </summary>
        public static string ReactorInputEquation(IReactorItem reactor, PieceDelta piece, bool detailed)
        {
            var mod = reactor.inputMod;
            if (!IsMeaningful(mod.modifier)) return string.Empty;

            var label = $"{mod.stat} {mod.modifier}";
            if (!detailed) return label;

            return InputField(mod.stat, piece, out var before, out var after)
                ? $"[base {before:0.###}] {mod.modifier} = {after:0.###}"
                : label;
        }

        // The WeaponStats field a reactor's inputMod targets — ProcChance has none (WeaponStatResolver
        // drops it silently), so it reports no backing field rather than a phantom 0 → 0.
        private static bool InputField(WeaponInputStat stat, PieceDelta piece, out float before, out float after)
        {
            switch (stat)
            {
                case WeaponInputStat.AttackSpeed:
                    before = piece.Before.AttackSpeed;
                    after  = piece.With.AttackSpeed;
                    return true;
                case WeaponInputStat.ManaCost:
                    before = piece.Before.ResourceCost;
                    after  = piece.With.ResourceCost;
                    return true;
                default:
                    before = after = 0f;
                    return false;
            }
        }

        /// <summary>
        /// The per-attachment <b>active-delta content</b> (tooltip-redesign spec §3, slice 4): the §3
        /// table's "active delta" column, read from the item's own modifiers/axis — not a chain-wide
        /// diff. This is the "what does this piece do?" answer for an attachment's <em>own</em> hover:
        /// <list type="bullet">
        ///   <item><b>Amplifier</b> — its output modifier, e.g. <c>Damage +6</c>.</item>
        ///   <item><b>Reactor</b> — the firing condition (<c>fires when hit</c>) plus any non-cost input
        ///   modifier (e.g. a Shifter-style <c>AttackSpeed +1</c>). A <em>resource-cost</em> input modifier
        ///   (e.g. <c>ManaCost * 120 %</c>) is <b>not</b> included here — it's <see cref="CostLine"/>'s
        ///   separately-tagged line instead (v2 slice 7, issue #23).</item>
        ///   <item><b>Shifter</b> — the input↔output economy trade (its own semantic identity line, always
        ///   shown as-is — not routed through <see cref="CostLine"/> even when its input targets a resource,
        ///   since the trade reads as one move, not a cost).</item>
        ///   <item><b>Converter</b> — the target it converts <em>to</em>, e.g. <c>→ Aoe</c> (the <em>from</em>
        ///   side is a Details/later-slice concern).</item>
        /// </list>
        /// <b>Additive</b>: a numeric line appears only when its modifier is non-default, so future fields
        /// don't force layout churn (spec §3 note). Non-attachments return an empty list.
        ///
        /// A <b>resource-cost</b> input modifier (e.g. a Reactor's <c>ManaCost * 120 %</c> trigger cost)
        /// is <b>not</b> included here — it's <see cref="CostLine"/>'s own separately-tagged line instead
        /// (v2 slice 7, issue #23), not folded into this generic list.
        ///
        /// <b>Details mode</b> (ADR-0010 Decision 3, issue #29): when <paramref name="detailed"/> is true
        /// and <paramref name="chain"/> places this item at a resolvable position, every numeric modifier
        /// resolves to <c>base → result</c> — the same positional diff <see cref="Pieces"/> already
        /// computes for the piece list — instead of printing <see cref="Modifier.ToString"/> directly.
        /// <paramref name="chain"/> is null for a loose (unchained) item, or when Details mode is off;
        /// either way the line falls back to the compact modifier form.
        /// </summary>
        public static IReadOnlyList<string> Describe(ITetrisItem item, IItemChain chain, bool detailed)
        {
            var piece = detailed ? OwnPiece(item, chain) : null;

            switch (item)
            {
                case IAmplifierItem amp:
                    return ModLine(amp.outputMod.stat, amp.outputMod.modifier, piece, detailed);

                case IShifterItem sh:
                    // The economy trade is one semantic move — the shifter's identity, always shown.
                    return new[]
                    {
                        $"{StatSegment(sh.inputMod.stat, sh.inputMod.modifier, piece, detailed)} ↔ " +
                        $"{StatSegment(sh.outputMod.stat, sh.outputMod.modifier, piece, detailed)}",
                    };

                case IReactorItem reactor:
                    var lines = new List<string> { $"fires {FiringCondition(reactor.ReactorType)}" };
                    // A resource-cost input mod (e.g. ManaCost) is CostLine's own row now (issue #23) —
                    // never folded into this generic line, even though ReactorInputEquation (the
                    // piece-list's shared formatter) still includes it there for that other presenter.
                    if (ResourceOf(reactor.inputMod.stat) == null)
                    {
                        var equation = piece.HasValue
                            ? ReactorInputEquation(reactor, piece.Value, detailed)
                            : IsMeaningful(reactor.inputMod.modifier)
                                ? $"{reactor.inputMod.stat} {reactor.inputMod.modifier}"
                                : string.Empty;
                        if (equation.Length > 0) lines.Add(equation);
                    }
                    return lines;

                case IConverterItem converter:
                    return new[] { $"→ {ConverterTarget(converter)}" };

                default:
                    return Array.Empty<string>();
            }
        }

        // This item's own marginal delta within chain, if it's a resolvable piece there — null when the
        // item is loose (no chain) or the chain carries no weapon (Pieces is then empty).
        private static PieceDelta? OwnPiece(ITetrisItem item, IItemChain chain)
        {
            if (chain == null) return null;
            foreach (var p in Pieces(chain))
                if (p.Item == item) return p;
            return null;
        }

        /// <summary>
        /// The chained-state's <b>cost line</b> (tooltip-redesign v2 slice 7, issue #23): a resource-cost
        /// input modifier — a Reactor's trigger cost, or an Amplifier/Converter's own <c>inputMod</c>
        /// (ADR-0009 / issue #25) — rendered as its own separately-tagged <c>[resource icon] ×N%</c> line,
        /// distinguishable from <see cref="Describe"/>'s generic stat-list lines rather than folded into
        /// them. Empty when the item carries no cost-eligible input modifier, its input doesn't target a
        /// resource-cost stat, or the modifier is a no-op (additive rule, mirrors <see cref="IsMeaningful"/>).
        ///
        /// Shifter is excluded even though it also carries an <c>inputMod</c>: its input↔output pair is
        /// one semantic economy-trade line in <see cref="Describe"/>, not a cost to call out separately
        /// (2026-07-02 scope note on issue #23: "any item carrying a non-default inputMod — Reactor,
        /// Amplifier, or Converter alike" — Shifter was never included in that widening).
        /// </summary>
        public static string CostLine(ITetrisItem item)
        {
            var mod = InputModOf(item);
            if (mod == null) return string.Empty;

            var resource = ResourceOf(mod.stat);
            if (resource == null || !IsMeaningful(mod.modifier)) return string.Empty;

            return $"{ResourceGlyphs.For(resource.Value)} {CostMultiplierText(mod.modifier)}";
        }

        // The inputMod-carrying families CostLine considers — Reactor's trigger cost plus Amplifier/
        // Converter's own inputMod (ADR-0009 / issue #25). Shifter deliberately excluded (see CostLine's
        // own doc); null for anything else (a bare Weapon, or an attachment with no inputMod at all).
        private static WeaponInputModifier InputModOf(ITetrisItem item) => item switch
        {
            IReactorItem reactor     => reactor.inputMod,
            IAmplifierItem amplifier => amplifier.inputMod,
            IConverterItem converter => converter.inputMod,
            _                        => null,
        };

        // The resource a WeaponInputStat's cost draws from — null when the stat isn't a resource cost at
        // all (AttackSpeed, ProcChance). Only ManaCost exists today (WeaponInputStat.cs: LifeCost was
        // retired per ADR-0005 §4 — Cost is one pool), so Health never actually resolves here yet.
        private static ResourceType? ResourceOf(WeaponInputStat stat) => stat switch
        {
            WeaponInputStat.ManaCost => ResourceType.Mana,
            _                        => null,
        };

        // "×N%"-shaped text (spec's own worked example: a Reactor's ×120% mana trigger cost). PercentMult
        // stores the authored percent directly (120 => ×120%). Other modifier types don't occur on a cost
        // stat in any authored config today; falling back to the modifier's own ToString avoids
        // fabricating a misleading "×" shape for a case that isn't real yet.
        private static string CostMultiplierText(Modifier mod) =>
            mod.Type == ModifierType.PercentMult ? $"×{(float)mod:0.###}%" : mod.ToString();

        /// <summary>
        /// Whether <paramref name="item"/> belongs to the chain's <b>upstream trigger family</b> —
        /// Reactor and Shifter, the two candidates <c>ChainResolver</c> walks upstream to find a chain's
        /// root (CLAUDE.md: "root ... resolved by walking upstream to the furthest trigger — a
        /// Shifter/Reactor — else the weapon itself") — as opposed to the <b>downstream magnitude-modifier
        /// family</b> (Amplifier, Converter). Issue #18: the weapon's piece list previously gave Shifter
        /// the same "both equations" numeric-diff framing as Amplifier, which misrepresented its role;
        /// this is the presentation-side classification the piece list groups by, so Shifter renders with
        /// Reactor's framing instead. Purely a rendering split — <c>ChainResolver</c> itself is untouched.
        /// </summary>
        public static bool IsUpstreamFamily(ITetrisItem item) => item is IReactorItem or IShifterItem;

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
        // internal so the sibling CompareBlock (slice 8) reuses the one map instead of duplicating it.
        internal static string ConverterTarget(IConverterItem c) => c.Axis switch
        {
            ConverterAxis.Delivery => c.ToDelivery.ToString(),
            ConverterAxis.Affinity => c.ToAffinity.ToString(),
            ConverterAxis.Anchor   => c.ToAnchor.ToString(),
            ConverterAxis.Resource => c.ToResource.ToString(),
            _                      => c.Axis.ToString(),
        };

        // Amplifier's output-modifier line, dropped when the modifier is a no-op (additive rule).
        private static IReadOnlyList<string> ModLine(WeaponOutputStat stat, Modifier mod, PieceDelta? piece, bool detailed) =>
            IsMeaningful(mod) ? new[] { StatSegment(stat, mod, piece, detailed) } : Array.Empty<string>();

        // "{stat} {modifier}", or under Details mode with a resolvable piece, "{stat} {before} → {after}"
        // — the same base→result shape Stat()/ReactorInputEquation already give the weapon-terminal and
        // piece-list paths (ADR-0010 Decision 3).
        private static string StatSegment(WeaponOutputStat stat, Modifier mod, PieceDelta? piece, bool detailed) =>
            detailed && piece.HasValue && OutputField(stat, piece.Value, out var before, out var after)
                ? $"{stat} {before:F1} → {after:F1}"
                : $"{stat} {mod}";

        private static string StatSegment(WeaponInputStat stat, Modifier mod, PieceDelta? piece, bool detailed) =>
            detailed && piece.HasValue && InputField(stat, piece.Value, out var before, out var after)
                ? $"{stat} {before:F1} → {after:F1}"
                : $"{stat} {mod}";

        // The WeaponStats field an Amplifier/Shifter's outputMod targets — mirrors InputField's shape.
        private static bool OutputField(WeaponOutputStat stat, PieceDelta piece, out float before, out float after)
        {
            switch (stat)
            {
                case WeaponOutputStat.Damage:
                    before = piece.Before.Damage;
                    after  = piece.With.Damage;
                    return true;
                default:
                    before = after = 0f;
                    return false;
            }
        }

        private const float Epsilon = 1e-4f;

        // A flat/percent-add modifier of ~0 changes nothing → not worth a line. Percent-mult / overwrite
        // are deliberate authored values (× x %, = x), so they always print.
        // internal so CompareBlock (slice 8) shares the same no-op gate the piece views use.
        internal static bool IsMeaningful(Modifier mod) => mod.Type switch
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

    /// <summary>Per-stat "did the chain touch this?" flags for the weapon's terminal totals (issue #19)
    /// — see <see cref="PositionalDelta.ChangedStats"/>.</summary>
    public readonly struct TerminalStats
    {
        public bool DamageChanged      { get; }
        public bool AttackSpeedChanged { get; }
        public bool CostChanged        { get; }

        public TerminalStats(bool damageChanged, bool attackSpeedChanged, bool costChanged)
        {
            DamageChanged      = damageChanged;
            AttackSpeedChanged = attackSpeedChanged;
            CostChanged        = costChanged;
        }
    }
}
