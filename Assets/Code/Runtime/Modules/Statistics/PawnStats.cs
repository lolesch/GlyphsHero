using System;
using Code.Data.Enums;
using Code.Data.Pawns;
using NaughtyAttributes;
using UnityEngine;

namespace Code.Runtime.Modules.Statistics
{
    [Serializable]
    public sealed class PawnStats : IPawnStats
    {
        [field: SerializeField, ReadOnly, AllowNesting] public Resource health    { get; private set; }
        [field: SerializeField, ReadOnly, AllowNesting] public Resource mana      { get; private set; }
        
        [field: SerializeField, ReadOnly, AllowNesting] public Stat healthRegen   { get; private set; }
        [field: SerializeField, ReadOnly, AllowNesting] public Stat manaRegen     { get; private set; }
        [field: SerializeField, ReadOnly, AllowNesting] public Stat movementSpeed { get; private set; }
        [field: SerializeField, ReadOnly, AllowNesting] public Stat range         { get; private set; }

        public PawnStats(PawnConfig config)
        {
            health      = new Resource(PawnStat.LifeMax, config.baseHealth);
            healthRegen = new Stat(PawnStat.LifeRegen,   config.baseHealthRegen);
            mana        = new Resource(PawnStat.ManaMax, config.baseMana);
            manaRegen   = new Stat(PawnStat.ManaRegen,   config.baseManaRegen);
            movementSpeed = new Stat(PawnStat.MovementSpeed, config.movementSpeed);
            range         = new Stat(PawnStat.Range, config.baseRange);
        }

        private Stat GetStat(PawnStat type) => type switch
        {
            PawnStat.LifeMax   => health,
            PawnStat.ManaMax   => mana,
            PawnStat.LifeRegen => healthRegen,
            PawnStat.ManaRegen => manaRegen,
            PawnStat.MovementSpeed => movementSpeed,
            PawnStat.Range     => range,
            PawnStat.None or _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public void ApplyMod(PawnStatModifier mod)  => GetStat(mod.PawnStat)?.AddModifier(mod.Modifier);
        public void RemoveMod(PawnStatModifier mod) => GetStat(mod.PawnStat)?.TryRemoveModifier(mod.Modifier);
    }

    public interface IPawnStats
    {
        Resource health      { get; }
        Resource mana        { get; }
        public Stat healthRegen   { get; }
        public Stat manaRegen     { get; }
        public Stat movementSpeed { get; }
        public Stat range         { get; }

        void ApplyMod(PawnStatModifier mod);
        void RemoveMod(PawnStatModifier mod);
    }
}