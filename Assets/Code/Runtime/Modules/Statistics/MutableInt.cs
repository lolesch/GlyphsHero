using System;
using Code.Data.Enums;
using NaughtyAttributes;
using UnityEngine;

namespace Code.Runtime.Modules.Statistics
{
    [Serializable]
    public sealed class MutableInt : IMutable<int>
    {
        [SerializeField, ReadOnly] private int totalValue;
        [SerializeField, ReadOnly] private MutableFloat mutableFloat;
        [SerializeField, ReadOnly] private RoundingMode roundingMode;

        public MutableInt( int baseValue, RoundingMode roundingMode = RoundingMode.Nearest )
        {
            mutableFloat = new MutableFloat( baseValue );
            this.roundingMode = roundingMode;
            totalValue = baseValue;
            OnTotalChanged = null;
        }
        
        public static implicit operator int( MutableInt mutableInt ) => mutableInt!.totalValue;
        
        public event Action<int> OnTotalChanged;

        public void AddModifier( Modifier modifier )
        {
            mutableFloat.AddModifier( modifier );
            CalculateTotalValue();
        }

        public bool TryRemoveModifier( Modifier modifier )
        {
            if( mutableFloat.TryRemoveModifier( modifier ) )
            {
                CalculateTotalValue();
                return true;
            }
            
            return false;
        }

        private void CalculateTotalValue()
        {
            var newTotal = Round( mutableFloat, roundingMode );
            //newTotal = Mathf.Clamp(newTotal, range.min, range.max);

            if( Mathf.Approximately( totalValue, newTotal ) )
                return;

            totalValue = newTotal;
            OnTotalChanged?.Invoke( totalValue );
        }

        private static int Round( float value, RoundingMode mode ) => mode switch
        {
            RoundingMode.Floor => Mathf.FloorToInt( value ),
            RoundingMode.Ceil  => Mathf.CeilToInt( value ),
            _                  => Mathf.RoundToInt( value ),
        };
    }
}