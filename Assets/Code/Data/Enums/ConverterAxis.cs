namespace Code.Data.Enums
{
    /// <summary>
    /// Which attack axis a <c>Converter</c> reclassifies (ADR-0004 §1, Converter.md). The Converter
    /// changes the <em>kind</em> on one axis, never the <em>amount</em> (that is the Amplifier) nor the
    /// <em>trade</em> between stats (the Shifter).
    ///
    /// v1 covers the three axes that already exist as data on <c>WeaponStats</c> and flow through
    /// <c>WeaponStatResolver</c>: <see cref="Delivery"/> (the geometry mask), <see cref="Affinity"/>
    /// (whose side), and <see cref="Anchor"/> (what the geometry centres on). The remaining axes from
    /// Converter.md — damage type, target strategy, resource type, trigger event — are deferred until
    /// their underlying systems are data-driven.
    /// </summary>
    public enum ConverterAxis
    {
        /// <summary>Reclassify the delivery pattern mask (e.g. Single → Cleave).</summary>
        Delivery,

        /// <summary>Reclassify the affinity (e.g. Hostile → Friendly).</summary>
        Affinity,

        /// <summary>Reclassify the anchor (e.g. Target → Origin).</summary>
        Anchor,
    }
}
