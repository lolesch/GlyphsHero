using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using NaughtyAttributes;
using UnityEngine;

namespace Code.Runtime.Modules.Statistics
{
    [Serializable]
    public sealed class MutableFloat : IMutable<float>, IFormattable
    {
        [SerializeField, ReadOnly] private float totalValue;
        [SerializeField, ReadOnly] private float baseValue;
        [SerializeField, ReadOnly] private List<Modifier> modifiers;
        
        public MutableFloat( float baseValue )
        {
            this.baseValue = baseValue;
            totalValue = baseValue;
            modifiers = new List<Modifier>();
            OnTotalChanged = null;
        }

        public static implicit operator float( MutableFloat mutableFloat ) => mutableFloat!.totalValue;

        public event Action<float> OnTotalChanged;

        /// <summary>
        /// An independent copy: a fresh modifier list and no subscribers. Mutating the clone (e.g.
        /// removing one modifier to answer "what would this be without it?") never touches this
        /// instance and never fires this instance's <see cref="OnTotalChanged"/>.
        /// </summary>
        public MutableFloat Clone()
        {
            var clone = new MutableFloat( baseValue ) { modifiers = new List<Modifier>( modifiers ) };
            clone.CalculateTotalValue();
            return clone;
        }

        public void AddModifier( Modifier modifier )
        {
            modifiers.Add( modifier );
            CalculateTotalValue();
        }

        public bool TryRemoveModifier( Modifier modifier ) => TryRemoveModifier( modifier, warnIfMissing: true );

        /// <summary>
        /// A caller that expects the modifier might already be gone — e.g. probing a clone — can
        /// pass <paramref name="warnIfMissing"/> false to skip the warning instead of spamming the
        /// console on an expected miss.
        /// </summary>
        public bool TryRemoveModifier( Modifier modifier, bool warnIfMissing )
        {
            for( var i = modifiers.Count; i-- > 0; )
                if( modifiers[i].Equals( modifier ) )
                {
                    modifiers.RemoveAt( i );

                    CalculateTotalValue();
                    return true;
                }

            if( warnIfMissing )
                Debug.LogWarning( $"Modifier {modifier} not found" );
            return false;
        }

        /*public bool TryRemoveAllModifiersBySource( Guid source )
        {
            var removed = false;
            for( var i = modifiers.Count; i-- > 0; )
            {
                if( modifiers[i].source != source ) 
                    continue;
                modifiers.RemoveAt( i );
                removed = true;
            }
            if( removed )
            {
                CalculateTotalValue();
                return true;
            }
            
            Debug.LogWarning( $"No modifiers of source {source} were found" );
            return false;
        }*/

        public string ToString(string format) => totalValue.ToString( format );
        public string ToString(string format, IFormatProvider provider) => totalValue.ToString( format, provider );
        
        private void CalculateTotalValue()
        {
            ApplyModifiers( out var newTotal );

            //newTotal = Mathf.Clamp(newTotal, range.min, range.max);

            if( Mathf.Approximately( totalValue, newTotal ) )
                return;

            totalValue = newTotal;
            OnTotalChanged?.Invoke( totalValue );
        }

        private void ApplyModifiers( out float newTotal )
        {
            newTotal = baseValue;
            if( !modifiers.Any() )
                return;

            var overwriteMods = modifiers.Where( x => x.Type == ModifierType.Overwrite )
                .OrderByDescending( x => x );
            if( overwriteMods.Any() )
            {
                newTotal = overwriteMods.FirstOrDefault();
                return;
            }

            var flatAddModValue = modifiers.Where( x => x.Type == ModifierType.FlatAdd ).Sum( x => x );
            newTotal += flatAddModValue;

            var percentAddModValue = modifiers.Where( x => x.Type == ModifierType.PercentAdd ).Sum( x => x / 100f );
            newTotal *= 1 + percentAddModValue;

            var percentMultMods = modifiers.Where( x => x.Type == ModifierType.PercentMult );
            newTotal = percentMultMods.Aggregate( newTotal, ( current, mod ) => current * ( 1 + mod / 100f ) );
        }
    }

    public interface IMutable<out T>
    {
        void AddModifier( Modifier modifier );
        bool TryRemoveModifier( Modifier modifier );
        event Action<T> OnTotalChanged;
    }
}