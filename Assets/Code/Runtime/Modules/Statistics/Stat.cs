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
        
        //public bool TryRemoveAllModifiersBySource( IModifierSource source ) => MaxValue.TryRemoveAllModifiersBySource( source.guid );

        public virtual Stat GetDeepCopy()
        {
            var other = (Stat) MemberwiseClone();
            other.name = string.Copy( name );
            other.pawnStat = pawnStat;
            other.MaxValue = MaxValue;

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