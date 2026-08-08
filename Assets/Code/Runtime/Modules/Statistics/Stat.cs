using System;
using Code.Data.Enums;
using NaughtyAttributes;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Runtime.Modules.Statistics
{
    [Serializable]
    public class Stat : IStat
    {
        [SerializeField, HideInInspector] protected string name;

        [field: FormerlySerializedAs("<StatType>k__BackingField")] [field: SerializeField, ReadOnly] public PawnStat pawnStat { get; protected set; }

        [SerializeField, ReadOnly] protected MutableFloat MaxValue;

        public Stat( PawnStat pawnStat, float baseValue )
        {
            this.pawnStat = pawnStat;
            MaxValue = new MutableFloat( baseValue );
        }

        public static implicit operator float( Stat stat ) => stat.MaxValue;

        /// <summary>
        /// Fires with the new total whenever a modifier changes this stat's value.
        /// Forwards to the wrapped <see cref="MaxValue"/> so <see cref="Stat"/> stays the sole
        /// modifier gate — <see cref="MutableFloat"/> itself is never handed out. Value reads use
        /// the implicit <c>float</c> operator.
        /// </summary>
        public event Action<float> OnTotalChanged
        {
            add => MaxValue.OnTotalChanged += value;
            remove => MaxValue.OnTotalChanged -= value;
        }

        public void AddModifier( Modifier modifier ) => MaxValue.AddModifier( modifier );
        public bool TryRemoveModifier( Modifier modifier ) => MaxValue.TryRemoveModifier( modifier );
        public bool TryRemoveModifier( Modifier modifier, bool warnIfMissing ) => MaxValue.TryRemoveModifier( modifier, warnIfMissing );

        //public bool TryRemoveAllModifiersBySource( IModifierSource source ) => MaxValue.TryRemoveAllModifiersBySource( source.guid );

        /// <summary>
        /// An independent copy — <see cref="MaxValue"/> is deep-copied (see <see cref="MutableFloat.Clone"/>),
        /// not aliased, so mutating the copy (e.g. removing a modifier to probe "what would this be
        /// without it?") never touches this instance.
        /// </summary>
        public virtual Stat GetDeepCopy()
        {
            var other = (Stat) MemberwiseClone();
            other.name = name; // string is immutable, plain assignment is already an independent copy
            other.pawnStat = pawnStat;
            other.MaxValue = MaxValue.Clone();

            return other;
        }

        public sealed override string ToString()
        {
            var statName = pawnStat.ToDescription();

            if( statName.Contains( "Percent" ) )
                statName = statName.Replace( " Percent", "%" );

            return $"{statName}: {MaxValue:0.###}";
        }
    }

    internal interface IStat
    {
        PawnStat pawnStat { get; }
        void AddModifier( Modifier modifier );
        bool TryRemoveModifier( Modifier modifier );
        Stat GetDeepCopy();
    }
    
    public interface IModifierSource {
        Guid guid { get; }
    }
}