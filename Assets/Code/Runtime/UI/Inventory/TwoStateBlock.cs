using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Runtime.Modules.Inventory;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>symmetric two-state model</b> (tooltip-redesign spec 2026-06-30, §2, slice 5):
    /// every item has two states and <em>both are always shown</em> — the live one emphasised, the other
    /// dim. This is the pure, Unity-free logic that decides <em>which two states</em> an item has and
    /// <em>which is active</em>; the presenter (the tooltip) supplies the bold/dim emphasis (the only
    /// state marker — there is no badge).
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
    /// attachment and <c>!isPayload</c> for a weapon. The Alt "before → after" math expansion is a later
    /// slice; this is the active-vs-other framing and the no-Alt content only.
    /// </summary>
    public static class TwoStateBlock
    {
        public static TwoStateView Build(ITetrisItem item, bool primaryActive)
        {
            switch (item)
            {
                case IWeaponItem weapon:
                {
                    var driving = new ItemStateView(ItemStateKind.Driving, "as driving weapon", DrivingLines(weapon));
                    var payload = new ItemStateView(ItemStateKind.Payload, "as payload", PayloadLines(weapon));
                    return primaryActive
                        ? new TwoStateView(driving, payload)
                        : new TwoStateView(payload, driving);
                }

                case IAmplifierItem:
                case IShifterItem:
                case IReactorItem:
                case IConverterItem:
                {
                    var chained   = new ItemStateView(ItemStateKind.Chained, "chained",
                        PositionalDelta.Describe(item));
                    var unchained = new ItemStateView(ItemStateKind.Unchained, "unchained",
                        AffixLines(item as IAttachmentItem));
                    return primaryActive
                        ? new TwoStateView(chained, unchained)
                        : new TwoStateView(unchained, chained);
                }

                default:
                    var empty = new ItemStateView(ItemStateKind.Chained, "", Array.Empty<string>());
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
        private static IReadOnlyList<string> AffixLines(IAttachmentItem attachment) =>
            attachment == null || attachment.affixes.Count == 0
                ? Array.Empty<string>()
                : attachment.affixes.Select(a => $"{a.PawnStat} {a.Modifier}").ToList();
    }

    /// <summary>Which of an item's two states a <see cref="ItemStateView"/> describes.</summary>
    public enum ItemStateKind { Chained, Unchained, Driving, Payload }

    /// <summary>
    /// One of an item's two states: its <see cref="Kind"/>, a player-facing <see cref="Label"/>, and the
    /// content <see cref="Lines"/> for that state (empty when the state carries nothing). Keeping the raw
    /// lines (rather than a formatted, emphasised string) is what makes the two-state model unit-testable
    /// without driving Unity — the presenter adds the bold/dim emphasis.
    /// </summary>
    public readonly struct ItemStateView
    {
        public ItemStateKind        Kind  { get; }
        public string               Label { get; }
        public IReadOnlyList<string> Lines { get; }

        public ItemStateView(ItemStateKind kind, string label, IReadOnlyList<string> lines)
        {
            Kind  = kind;
            Label = label;
            Lines = lines;
        }
    }

    /// <summary>An item's two states with the live one already resolved to <see cref="Active"/>.</summary>
    public readonly struct TwoStateView
    {
        public ItemStateView Active { get; }
        public ItemStateView Other  { get; }

        public TwoStateView(ItemStateView active, ItemStateView other)
        {
            Active = active;
            Other  = other;
        }
    }
}
