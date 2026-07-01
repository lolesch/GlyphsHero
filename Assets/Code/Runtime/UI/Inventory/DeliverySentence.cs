using System;
using Code.Data.Enums;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// Turns a delivery's three axes (tooltip-redesign spec 2026-06-30, slice 2) into one verb-led,
    /// readable sentence — <see cref="DeliveryPattern"/> (geometry) × <see cref="Affinity"/> (whose side)
    /// × <see cref="Anchor"/> (where it centres), plus the Aoe radius. Replaces the old robot output
    /// (<c>Single · hits enemies · on target</c>) the tooltip used to print.
    ///
    /// Pure and fully unit-testable: every axis combination maps to exactly one string. The verb is led
    /// by affinity-and-pattern; the subject by affinity-and-pattern (singular for the anchor-locked
    /// Single, plural for the area patterns); the location by the anchor (and the Aoe radius). Two
    /// collapses keep the wording honest: <see cref="Affinity.Self"/> only ever lands on the caster, and
    /// a Single anchored on the origin covers only the caster's own hex — both read as "self".
    /// </summary>
    public static class DeliverySentence
    {
        public static string Build(DeliveryPattern delivery, Affinity affinity, Anchor anchor, int shapeSize)
        {
            var pattern = Dominant(delivery);

            if (pattern == DeliveryPattern.None)
                return "Has no delivery";

            // Affinity.Self ignores geometry — it only ever lands on the caster.
            if (affinity == Affinity.Self)
                return "Buffs self";

            // A Single delivery anchored on the origin covers only the caster's hex: subject and
            // location merge into "self", whatever the affinity (Hostile here = a deliberate self-hurt).
            if (pattern == DeliveryPattern.Single && anchor == Anchor.Origin)
                return $"{SelfVerb(affinity)} self";

            var verb       = Verb(affinity, pattern);
            var who        = pattern == DeliveryPattern.Single ? Singular(affinity) : Plural(affinity);
            var anchorNoun = anchor == Anchor.Origin ? "self" : "the target";

            return pattern switch
            {
                DeliveryPattern.Single => $"{verb} {who} at {anchorNoun}",
                DeliveryPattern.Aoe    => $"{verb} {who} within {Math.Max(shapeSize, 0)} of {anchorNoun}",
                DeliveryPattern.Cleave => $"{verb} {who} around {anchorNoun}",
                DeliveryPattern.Line   => $"{verb} {who} in a line to {anchorNoun}",
                _                      => $"{verb} {who} at {anchorNoun}",
            };
        }

        // Delivery is a stackable [Flags] mask (covered hexes are the union). A single sentence describes
        // the most expansive set flag — the one that dominates the read — so a combined mask still yields
        // one grammatical line. Priority: Aoe (disk) ▸ Line (beam) ▸ Cleave (arc) ▸ Single (one hex).
        private static DeliveryPattern Dominant(DeliveryPattern delivery)
        {
            if (delivery.HasFlag(DeliveryPattern.Aoe))    return DeliveryPattern.Aoe;
            if (delivery.HasFlag(DeliveryPattern.Line))   return DeliveryPattern.Line;
            if (delivery.HasFlag(DeliveryPattern.Cleave)) return DeliveryPattern.Cleave;
            if (delivery.HasFlag(DeliveryPattern.Single)) return DeliveryPattern.Single;
            return DeliveryPattern.None;
        }

        private static string Verb(Affinity affinity, DeliveryPattern pattern) =>
            affinity == Affinity.Friendly
                ? pattern == DeliveryPattern.Single ? "Buffs" : "Heals"
                : pattern switch // Hostile (and any non-friendly default)
                {
                    DeliveryPattern.Single => "Strikes",
                    DeliveryPattern.Line   => "Pierces",
                    DeliveryPattern.Cleave => "Cleaves",
                    DeliveryPattern.Aoe    => "Blasts",
                    _                      => "Strikes",
                };

        // The collapsed self-only verb: a friendly self-hit buffs, a hostile one hurts.
        private static string SelfVerb(Affinity affinity) =>
            affinity == Affinity.Hostile ? "Hurts" : "Buffs";

        private static string Singular(Affinity affinity) =>
            affinity == Affinity.Friendly ? "an ally" : "a single enemy";

        private static string Plural(Affinity affinity) =>
            affinity == Affinity.Friendly ? "allies" : "enemies";
    }
}
