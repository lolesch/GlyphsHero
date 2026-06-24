using System;

namespace Code.Data.Enums
{
    /// <summary>
    /// How an attack covers hexes — the Delivery Pattern axis (CONTEXT.md, ADR-0002/0004). The
    /// <b>geometry</b> only: a weapon (or a payload's child delivery) carries a stackable <b>mask</b>
    /// and the covered hexes are the union of each set flag's contribution (resolved by
    /// <c>DeliveryResolver</c>). Damage is hex-occupancy-resolved — every pawn standing on a covered
    /// hex is hit, filtered by the delivery's <see cref="Affinity"/> (the caller's concern; the pattern
    /// itself is team-agnostic).
    ///
    /// The size-free patterns scale by engagement distance and are gated only by the pawn's Reach
    /// (the acquisition stat, ADR-0001); there is no separate "shape size" knob for them. Only
    /// <see cref="Aoe"/> carries an explicit radius, and it is payload-only — no weapon paints a disk.
    /// </summary>
    [Flags]
    public enum DeliveryPattern
    {
        None   = 0,
        Single = 1 << 0,  // the aim anchor's hex (the locked target)
        Line   = 1 << 1,  // every hex from the firing pawn out to the anchor (a beam)
        Cleave = 1 << 2,  // the anchor + its two same-ring neighbours — a 3-hex arc (aka "Swipe")
        // 1 << 3 reserved (was Self — moved to the Affinity axis, ADR-0004 §3)
        Aoe    = 1 << 4,  // a disk of shapeSize radius around the anchor (payload-only)
    }
}
