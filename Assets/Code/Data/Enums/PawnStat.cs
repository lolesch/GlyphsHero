namespace Code.Data.Enums
{
    public enum PawnStat : byte
    {
        None = 0,
        
        LifeMax,
        LifeRegen,
        
        ManaMax,
        ManaRegen,
        
        MovementSpeed,

        // Reach ceiling for range-scaling weapon deliveries (ADR-0001, Decision 2).
        // Range is a pawn stat, not a weapon stat: capped + expensive, never freely
        // Amplifier-pumpable. Movement closes to the minimum active-weapon reach.
        Range,


        // WEAPON BONI
        // ResourceCostReduction,
        // CooldownReduction,
        // AdditionalDamage, // weapon base damage? TBD
        // Leech?
        
        // Defense Layer TBD
        // Presence <- influences target finding, so tanks have high presence therefore attract enemies.
        // sight radius => implement fog of war
    }
}