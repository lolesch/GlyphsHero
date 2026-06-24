using System;
using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Weapon
{
    /// <summary>
    /// Base class for all payload hex-grid effects.
    /// Subclasses are serialized inline via [SerializeReference] on PayloadBehavior.Effects.
    /// </summary>
    [Serializable]
    public abstract class PayloadEffect { }

    /// <summary>
    /// Gives the caster resources per target hit (leech / flat gen-on-hit). Replaces the old
    /// WeaponConfig.ResourceGenOnHit weapon field (ADR-0005 §3) — gain is now an on-hit effect
    /// so it is grantable and stackable like any other modifier and its pool is independent of Cost.
    /// Author it in the weapon's Payload.Effects list; PawnCombatController.ExecuteEffect applies it.
    /// Pool resolution and IncreaseCurrent live in combat (Runtime assembly), not here.
    /// </summary>
    [Serializable]
    public sealed class ResourcePayloadEffect : PayloadEffect
    {
        [field: SerializeField] public ResourceType Pool            { get; private set; }
        [field: SerializeField] public float        PercentOfDamage { get; private set; }
        [field: SerializeField] public float        FlatAmount      { get; private set; }

        public ResourcePayloadEffect() { }

        public ResourcePayloadEffect(ResourceType pool, float percentOfDamage = 0f, float flatAmount = 0f)
        {
            Pool            = pool;
            PercentOfDamage = percentOfDamage;
            FlatAmount      = flatAmount;
        }

        /// <summary>
        /// Returns how much of the given resource to restore for one target hit.
        /// Leech (% of damage) takes priority when both fields are set; flat is the degenerate form.
        /// Pool selection and IncreaseCurrent are handled by the caller (PawnCombatController).
        /// </summary>
        public float ComputeGain(float damageDealt) =>
            PercentOfDamage > 0f ? damageDealt * PercentOfDamage / 100f : FlatAmount;
    }

    /// <summary>Applies a status effect to all pawns in the payload's target shape.</summary>
    [Serializable]
    public sealed class StatusPayloadEffect : PayloadEffect
    {
        [field: SerializeField] public StatusEffect Status { get; private set; }
    }

    /// <summary>Displaces or controls pawns in the payload's target shape.</summary>
    [Serializable]
    public sealed class PositionPayloadEffect : PayloadEffect
    {
        [field: SerializeField] public PositionEffectType EffectType { get; private set; }
        /// <summary>Hex distance for Push/Pull. Ignored by Stun.</summary>
        [field: SerializeField] public int Distance { get; private set; }
    }

    /// <summary>Writes a terrain type onto all hexes in the payload's target shape.</summary>
    [Serializable]
    public sealed class TerrainPayloadEffect : PayloadEffect
    {
        [field: SerializeField] public TerrainType TerrainType { get; private set; }
    }

    public enum PositionEffectType
    {
        Push,
        Pull,
        Stun,
    }
}
