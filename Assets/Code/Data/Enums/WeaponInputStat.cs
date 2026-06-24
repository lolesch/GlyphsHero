namespace Code.Data.Enums
{
    public enum WeaponInputStat : byte
    {
        AttackSpeed = 0,
        // LifeCost = 1 retired (ADR-0005 §4 — Cost is one pool, no half-built typed-cost pair)
        ManaCost   = 2,
        ProcChance = 3,
    }
}