namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// The tooltip's <b>universal stat channel</b> (tooltip-redesign v2 slice 8, issue #24): a leading
    /// glyph for any renderable numeric stat, so a stat line reads <c>♥ +10</c> instead of a bare number.
    /// Mirrors <see cref="TypeGlyphs"/>/<see cref="StateGlyphs"/>/<see cref="ResourceGlyphs"/>'s pattern
    /// (pure static map + TMP-font verification + ASCII fallback) rather than inventing a fourth icon
    /// system. LifeMax/ManaMax deliberately reuse <see cref="ResourceGlyphs"/>' ♥/✦ (same precedent as
    /// <see cref="StateGlyphs"/> reusing <see cref="TypeGlyphs"/>' ⚔/◈) — LifeRegen/ManaRegen get the
    /// outline variant of the same symbol.
    ///
    /// <see cref="StatKind"/> is a presentation-layer taxonomy, not a domain enum reuse: the domain has no
    /// single type spanning every renderable stat (<c>PawnStat</c> covers pawn passives, <c>WeaponOutputStat</c>
    /// only <c>Damage</c>, <c>WeaponInputStat</c> the trigger inputs including its resource-cost stat). One
    /// narrow UI-only enum here (rather than three parallel glyph maps, or leaking a shared domain enum into
    /// this layer) is a two-way door — cheap to rename or fold away later.
    ///
    /// VERIFIED in the Unity Editor (2026-08-08, via TMP_FontAsset.HasCharacter against the tooltip's
    /// LiberationSans SDF + fallback chain): every glyph below is already baked into the static atlas, same
    /// as the existing TypeGlyphs/StateGlyphs/ResourceGlyphs set — no missing-glyph-box risk found.
    /// </summary>
    public enum StatKind
    {
        Damage,
        Cost,
        AttackSpeed,
        ProcChance,
        LifeMax,
        LifeRegen,
        ManaMax,
        ManaRegen,
        MovementSpeed,
        Range,
    }

    public static class StatGlyphs
    {
        // Flip to true (no other change) only if a future font swap drops one of these from the atlas —
        // see this file's own VERIFY note above for the check already performed.
        public const bool UseAsciiFallback = false;

        public static string For(StatKind stat) =>
            UseAsciiFallback ? Ascii(stat) : Glyph(stat);

        /// <summary>
        /// Composes a stat line: <b>default mode</b> is glyph + value (<c>♥ +10</c>); <b>Details mode</b>
        /// additionally appends the stat's own name (<c>♥ +10 LifeMax</c>) for a player who hasn't learned
        /// the glyph vocabulary yet — added whenever <paramref name="detailed"/> is true, regardless of
        /// whether <paramref name="valueText"/> itself expanded into a base → result equation (that's the
        /// caller's call, not this composer's). <paramref name="valueText"/> is the caller's
        /// already-formatted number (sign, decimals, units) — this builder only owns the icon + label
        /// composition, not number formatting.
        /// </summary>
        public static string Format(StatKind stat, string valueText, bool detailed) =>
            detailed ? $"{For(stat)} {valueText} {stat}" : $"{For(stat)} {valueText}";

        private static string Glyph(StatKind stat) => stat switch
        {
            StatKind.Damage        => "✺", // damage
            StatKind.Cost          => "$", // resolved cost value (CostLine's own ×N% row stays ResourceGlyphs-specific)
            StatKind.AttackSpeed   => "⏱", // attack speed / fire rate
            StatKind.ProcChance    => "%", // proc chance
            StatKind.LifeMax       => "♥", // life max (= ResourceGlyphs' Health glyph)
            StatKind.LifeRegen     => "♡", // life regen
            StatKind.ManaMax       => "✦", // mana max (= ResourceGlyphs' Mana glyph)
            StatKind.ManaRegen     => "✧", // mana regen
            StatKind.MovementSpeed => "➤", // movement speed
            StatKind.Range         => "◎", // range
            _                      => string.Empty,
        };

        private static string Ascii(StatKind stat) => stat switch
        {
            StatKind.Damage        => "DMG",
            StatKind.Cost          => "CST",
            StatKind.AttackSpeed   => "SPD",
            StatKind.ProcChance    => "PROC",
            StatKind.LifeMax       => "HP",
            StatKind.LifeRegen     => "HP+",
            StatKind.ManaMax       => "MP",
            StatKind.ManaRegen     => "MP+",
            StatKind.MovementSpeed => "MOV",
            StatKind.Range         => "RNG",
            _                      => string.Empty,
        };
    }
}
