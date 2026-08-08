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

        // PawnStat.None is the "no passive stat" sentinel; treat it as a no-op rather than letting
        // GetStat throw, so an attachment without a passive can never crash stat application.
        public void ApplyMod(PawnStatModifier mod)
        {
            if (mod.PawnStat == PawnStat.None) return;
            GetStat(mod.PawnStat)?.AddModifier(mod.Modifier);
        }

        public void RemoveMod(PawnStatModifier mod)
        {
            if (mod.PawnStat == PawnStat.None) return;
            GetStat(mod.PawnStat)?.TryRemoveModifier(mod.Modifier);
        }

        /// <summary>
        /// Non-mutating "what would this stat be without/with this affix" (tooltip issue #22
        /// owned-context Details mode). Works uniformly whether <paramref name="mod"/> is currently
        /// live on the real stat (a loose affix, applied by <c>ChainStateController.OnUnchained</c>)
        /// or currently suppressed (a chained item's affix) — cloning the real stat first means the
        /// clone starts from whichever state is actually live, then a quiet remove normalizes it to
        /// "without" before re-adding for "with", so the pair is always a clean diff against the same
        /// rest-of-list baseline. Never touches the live <see cref="Stat"/>.
        /// </summary>
        public (float before, float after) PreviewAffix(PawnStatModifier mod)
        {
            if (mod.PawnStat == PawnStat.None) return (0f, 0f);

            var probe = GetStat(mod.PawnStat).GetDeepCopy();
            probe.TryRemoveModifier(mod.Modifier, warnIfMissing: false);
            var before = (float)probe;

            probe.AddModifier(mod.Modifier);
            var after = (float)probe;

            return (before, after);
        }
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
        (float before, float after) PreviewAffix(PawnStatModifier mod);
    }
}