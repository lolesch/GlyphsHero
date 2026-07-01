using System;
using Code.Data.Enums;
using NaughtyAttributes;
using UnityEngine;

namespace Code.Runtime.Modules.Statistics
{
    [Serializable]
    public sealed class Resource : Stat, IResource
    {
        public Resource( PawnStat resource, /*Stat regen,*/ float baseValue ) : base( resource, baseValue )
        {
            CurrentValue = baseValue;
            MaxValue.OnTotalChanged += _ => SetCurrentTo( CurrentValue );
        }

        //Regen = regen;
        [field: SerializeField, ReadOnly] public float CurrentValue { get; private set; }
        //[field: SerializeField, ReadOnly] public Stat Regen { get; private set; }

        public bool IsDepleted => CurrentValue <= 0;
        public bool IsFull => CurrentValue >= MaxValue;
        public float MissingValue => MaxValue - CurrentValue;
        public float Percentage => MaxValue <= 0f ? 0f : CurrentValue / MaxValue;
        public event Action<float, float, float> OnCurrentChanged; // (previous, newValue, total)
        public event Action OnDepleted;
        public event Action OnRecharged;

        public bool CanSpend( float amount ) => pawnStat == PawnStat.LifeMax
            ? amount < CurrentValue // prevent deplete health when using health as a resource
            : amount <= CurrentValue;

        /// <summary>Tries to add the amount to the current value.</summary>
        /// <returns>The remaining amount that was not added</returns>
        public float IncreaseCurrent( float amountToAdd )
        {
            if( amountToAdd < 0 )
                Debug.LogWarning( $"Amount to add ({amountToAdd}) must be positive" );

            var added = Math.Min( MissingValue, amountToAdd );
            //var overshoot = amountToAdd - added;

            if( added != 0 )
                SetCurrentTo( CurrentValue + added );

            return amountToAdd - added;
        }

        /// <summary>
        /// Regenerates current by <paramref name="ratePerSecond"/> over <paramref name="deltaTime"/>
        /// seconds, clamped at max. Driven per tick by the combat clock (the rate is the pawn's
        /// healthRegen/manaRegen stat). A non-positive rate or delta is a no-op.
        /// </summary>
        public void Regenerate( float ratePerSecond, float deltaTime )
        {
            if( ratePerSecond <= 0f || deltaTime <= 0f )
                return;

            IncreaseCurrent( ratePerSecond * deltaTime );
        }

        /// <summary>Tries to remove the amount from the current value</summary>
        /// <returns>The remaining amount that was not removed</returns>
        public float ReduceCurrent( float amountToRemove )
        {
            if( amountToRemove < 0 )
                Debug.LogWarning( $"Amount to remove ({amountToRemove}) must be positive" );

            var removed = Math.Min( CurrentValue, amountToRemove );

            if( removed != 0 )
                SetCurrentTo( CurrentValue - removed );

            return amountToRemove - removed;
        }

        public void RefillCurrent() => SetCurrentTo( MaxValue );
        //public void DepleteCurrent() => SetCurrentTo(0);

        public override Stat GetDeepCopy()
        {
            var other = (Resource) MemberwiseClone();
            other.name = string.Copy( name );
            other.pawnStat = pawnStat;
            other.MaxValue = MaxValue;
            other.CurrentValue = CurrentValue;
            other.OnCurrentChanged = null; //have no listeners to these deep copies

            return other;
        }

        private void SetCurrentTo( float value )
        {
            var newCurrent = Mathf.Clamp( value, 0, MaxValue );

            var previousValue = CurrentValue;
            CurrentValue = newCurrent;
            
            OnCurrentChanged?.Invoke( previousValue, CurrentValue, MaxValue );

            if( IsDepleted )
                OnDepleted?.Invoke();
            else if( IsFull )
                OnRecharged?.Invoke();
        }

        public Resource GetResourceCopy()
        {
            var other = (Resource) MemberwiseClone();
            other.name = string.Copy( name );
            other.pawnStat = pawnStat;
            other.MaxValue = MaxValue;
            other.OnCurrentChanged = null; //have no listeners to these deep copies

            return other;
        }
    }

    internal interface IResource : IStat
    {
        float CurrentValue { get; }
        bool IsDepleted { get; }
        bool IsFull { get; }
        float MissingValue { get; }

        event Action<float, float, float> OnCurrentChanged;
        event Action OnDepleted;
        event Action OnRecharged;

        bool CanSpend( float amount );
        float IncreaseCurrent( float amountToAdd );

        float ReduceCurrent( float amountToRemove );
        //void RefillCurrent();
        //void DepleteCurrent();
    }
}