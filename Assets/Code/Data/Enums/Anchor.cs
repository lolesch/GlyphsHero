namespace Code.Data.Enums
{
    /// <summary>
    /// What a delivery's geometry centres on — the Anchor axis (ADR-0004 §3). Independent of the
    /// delivery <see cref="DeliveryPattern"/> (geometry), <see cref="Affinity"/> (whose side), and the
    /// pawn's Reach (acquisition). <see cref="Target"/> centres on the chosen target; <see cref="Origin"/>
    /// centres on the firing pawn.
    ///
    /// Decoupled from Affinity (ADR-0004 §3 — the deferred "independent Anchor" axis): anchor-self now
    /// combines with <em>any</em> affinity, so anchor-Origin + Hostile + Aoe = a damage nova, anchor-Origin
    /// + Friendly = a heal-around-me, anchor-Origin + Self = the deliberate self-hurt build-around. The
    /// v1 coupling (a <see cref="Affinity.Self"/> delivery was implicitly self-anchored) is gone — a
    /// self-hurt weapon now authors <see cref="Origin"/> explicitly.
    /// </summary>
    public enum Anchor
    {
        /// <summary>Default — the geometry centres on the chosen target's hex.</summary>
        Target,

        /// <summary>The geometry centres on the firing pawn's own hex (self/origin).</summary>
        Origin,
    }
}
