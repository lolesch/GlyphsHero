using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>symmetric two-state model</b> (tooltip-redesign spec 2026-06-30, §2, slice 5):
    /// every item has two states and <em>both are always shown</em> — the live one emphasised, the other
    /// dim. This is the pure, Unity-free logic that decides <em>which two states</em> an item has and
    /// <em>which is active</em>; the presenter (the tooltip) supplies the bold/dim emphasis.
    ///
    /// The two states by item family:
    /// <list type="bullet">
    ///   <item><b>Attachment</b> (amplifier / shifter / reactor / converter) — <see cref="ItemStateKind.Chained"/>
    ///   (its live-in-a-chain delta, from <see cref="PositionalDelta.Describe"/>) vs
    ///   <see cref="ItemStateKind.Unchained"/> (its loose <see cref="IAttachmentItem.affixes"/> pawn-stat
    ///   affix). In a chain the chained effect is live and the affix suppressed; standalone it is the
    ///   reverse (ADR-0004 item roles).</item>
    ///   <item><b>Weapon</b> — <see cref="ItemStateKind.Driving"/> (fires the chain on its own stats) vs
    ///   <see cref="ItemStateKind.Payload"/> (carried downstream, delivering its own child pattern). A
    ///   weapon is driving when it is the chain's root weapon, payload when a weapon precedes it.</item>
    /// </list>
    ///
    /// <paramref name="primaryActive"/> tells the builder whether the <em>primary</em> state (chained for
    /// an attachment, driving for a weapon) is the live one, so the caller passes <c>isChained</c> for an
    /// attachment and <c>!isPayload</c> for a weapon. Render <b>position</b> is fixed regardless of which
    /// side is live (issue #20): <see cref="TwoStateView.Default"/> is always Unchained/Driving,
    /// <see cref="TwoStateView.Secondary"/> is always Chained/Payload — only <see cref="ItemStateView.IsActive"/>
    /// tracks <paramref name="primaryActive"/>, so the layout no longer jumps when the live side flips.
    /// </summary>
    public static class TwoStateBlock
    {
        /// <summary>
        /// <paramref name="chain"/> and <paramref name="detailed"/> only matter for the attachment
        /// branch (issue #29 / ADR-0010 Decision 3) — they feed <see cref="PositionalDelta.Describe"/>
        /// so the Chained line can resolve to <c>base → result</c> under Details mode. The weapon branch
        /// ignores both, so weapon-only callers can omit them.
        /// </summary>
        /// <param name="isOwned">Whether the item sits in a pawn's own grid versus an ownerless
        /// container (the stash, tooltip issue #22). Only the Unchained affix content reacts — there
        /// is no pawn to diff a stash item's affix against, so Details mode there adds descriptive
        /// text instead of a fabricated before→after equation. Defaults to owned so every existing
        /// call site (none of which pass it yet) keeps today's behavior unchanged.</param>
        /// <param name="stats">The owning pawn's live stats (issue #22) — only read when
        /// <paramref name="isOwned"/> and <paramref name="detailed"/> are both true, to compute the
        /// affix's real before→after via <see cref="IPawnStats.PreviewAffix"/>. Null falls back to the
        /// flat value line.</param>
        public static TwoStateView Build(ITetrisItem item, bool primaryActive, IItemChain chain = null,
            bool detailed = false, bool isOwned = true, IPawnStats stats = null)
        {
            switch (item)
            {
                case IWeaponItem weapon:
                {
                    var driving = new ItemStateView(ItemStateKind.Driving, StateGlyphs.For(ItemStateKind.Driving),
                        DrivingLines(weapon), isActive: primaryActive);
                    var payload = new ItemStateView(ItemStateKind.Payload, StateGlyphs.For(ItemStateKind.Payload),
                        PayloadLines(weapon), isActive: !primaryActive);
                    return new TwoStateView(driving, payload);
                }

                case IAmplifierItem:
                case IShifterItem:
                case IReactorItem:
                case IConverterItem:
                {
                    var unchained = new ItemStateView(ItemStateKind.Unchained, StateGlyphs.For(ItemStateKind.Unchained),
                        AffixLines(item as IAttachmentItem, detailed, isOwned, stats), isActive: !primaryActive);
                    var chained = new ItemStateView(ItemStateKind.Chained, StateGlyphs.For(ItemStateKind.Chained),
                        PositionalDelta.Describe(item, chain, detailed), isActive: primaryActive);
                    return new TwoStateView(unchained, chained);
                }

                default:
                    var empty = new ItemStateView(ItemStateKind.Chained, "", Array.Empty<string>(), isActive: true);
                    return new TwoStateView(empty, empty);
            }
        }

        // A weapon firing the chain: its own base attack — damage + delivery sentence over its base axes.
        private static IReadOnlyList<string> DrivingLines(IWeaponItem w) => new[]
        {
            $"{(float)w.Damage:F1} dmg",
            DeliverySentence.Build(w.Delivery, w.Affinity, w.Anchor, 0),
        };

        // The same weapon carried as a payload: its own damage delivered by its PayloadBehavior child
        // pattern (defaults mirror AppendPayloadOutput when the weapon carries no authored behaviour).
        private static IReadOnlyList<string> PayloadLines(IWeaponItem w)
        {
            var b         = w.Payload;
            var delivery  = b?.Delivery  ?? DeliveryPattern.Single;
            var affinity  = b?.Affinity  ?? Affinity.Hostile;
            var anchor    = b?.Anchor    ?? Anchor.Target;
            var shapeSize = b?.ShapeSize ?? 1;
            return new[]
            {
                $"{(float)w.Damage:F1} dmg",
                DeliverySentence.Build(delivery, affinity, anchor, shapeSize),
            };
        }

        // The loose (unchained) pawn-stat affixes an attachment applies when it sits alone in the grid.
        // Empty when the item carries no affix (or isn't an IAttachmentItem) — the presenter shows a dim
        // placeholder rather than a phantom line.
        //
        // Ownerless (stash) Details mode never expands into a before→after equation — there is no
        // owning pawn's live stat to diff against, and fabricating one (e.g. "0 → +10") would misread
        // as a real pawn floor (tooltip issue #22). It gets descriptive text instead. Owned-context
        // Details mode expands into the pawn's real before→after, read off IPawnStats.PreviewAffix —
        // that math is correct whether the affix is currently applied (item unchained) or currently
        // suppressed by a chain (item chained), since PreviewAffix always diffs against the rest of the
        // stat's modifier list. A null stats (no owner wired yet) falls back to the same flat line as
        // the default read, never a guess.
        private static IReadOnlyList<string> AffixLines(IAttachmentItem attachment, bool detailed, bool isOwned,
            IPawnStats stats)
        {
            if (attachment == null || attachment.affixes.Count == 0)
                return Array.Empty<string>();

            return attachment.affixes.Select(a =>
            {
                if (!detailed)
                    return $"{a.PawnStat} {a.Modifier}";

                if (!isOwned)
                    return $"{a.PawnStat} {a.Modifier} — {PawnStatDescription(a.PawnStat)}";

                if (stats == null)
                    return $"{a.PawnStat} {a.Modifier}";

                var (before, after) = stats.PreviewAffix(a);
                return $"{a.PawnStat} {before:0.###} → {after:0.###}";
            }).ToList();
        }

        // Player-facing "what does this stat do" text — ownerless Details mode's only addition, since
        // it has no pawn to compute a real before→after against (tooltip issue #22).
        private static string PawnStatDescription(PawnStat stat) => stat switch
        {
            PawnStat.LifeMax       => "increases max health",
            PawnStat.LifeRegen     => "increases health regen per second",
            PawnStat.ManaMax       => "increases max mana",
            PawnStat.ManaRegen     => "increases mana regen per second",
            PawnStat.MovementSpeed => "increases movement speed",
            PawnStat.Range         => "increases weapon reach ceiling",
            _                      => string.Empty,
        };
    }

    /// <summary>Which of an item's two states a <see cref="ItemStateView"/> describes.</summary>
    public enum ItemStateKind { Chained, Unchained, Driving, Payload }

    /// <summary>
    /// One of an item's two states: its <see cref="Kind"/>, a player-facing glyph <see cref="Label"/>, the
    /// content <see cref="Lines"/> for that state (empty when the state carries nothing), and whether this
    /// is the currently-live state (<see cref="IsActive"/>). Keeping the raw lines (rather than a
    /// formatted, emphasised string) is what makes the two-state model unit-testable without driving
    /// Unity — the presenter reads <see cref="IsActive"/> to add the bold/dim emphasis.
    /// </summary>
    public readonly struct ItemStateView
    {
        public ItemStateKind        Kind     { get; }
        public string               Label    { get; }
        public IReadOnlyList<string> Lines   { get; }
        public bool                 IsActive { get; }

        public ItemStateView(ItemStateKind kind, string label, IReadOnlyList<string> lines, bool isActive)
        {
            Kind     = kind;
            Label    = label;
            Lines    = lines;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// An item's two states in <b>fixed render position</b> (issue #20) — <see cref="Default"/> is always
    /// Unchained (attachment) / Driving (weapon), <see cref="Secondary"/> is always Chained (attachment) /
    /// Payload (weapon), secondary rendered below a divider. Which one is live moves with
    /// <c>primaryActive</c> via <see cref="ItemStateView.IsActive"/>, not position. <see cref="Active"/>/
    /// <see cref="Other"/> are convenience lookups for callers that only need "the live one" or "the dim
    /// one" without caring which fixed slot it landed in.
    /// </summary>
    public readonly struct TwoStateView
    {
        public ItemStateView Default   { get; }
        public ItemStateView Secondary { get; }

        public TwoStateView(ItemStateView @default, ItemStateView secondary)
        {
            Default   = @default;
            Secondary = secondary;
        }

        public ItemStateView Active => Default.IsActive ? Default : Secondary;
        public ItemStateView Other  => Default.IsActive ? Secondary : Default;
    }
}
