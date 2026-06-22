using System.Collections.Generic;
using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Weapon
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = Const.ItemConfig + "Weapon")]
    public sealed class WeaponConfig : ItemConfig
    {
        [field: Header("Weapon Properties")]
        [field: SerializeField] public float BaseDamage   { get; private set; }
        [field: SerializeField] public float AttackSpeed  { get; private set; }
        [field: SerializeField] public float ResourceCost { get; private set; }
        [field: SerializeField] public float ResourceGenOnHit { get; private set; }

        // The weapon's root delivery pattern mask (stackable). Default Single = hit the locked target
        // (current behavior). Weapons scale by the pawn's Reach and never author Aoe (payload-only).
        [field: SerializeField] public DeliveryPattern Delivery { get; private set; } = DeliveryPattern.Single;

        public WeaponTags tags;
        public int range;
        
        [field: Header("Payload")]
        [field: SerializeField] public PayloadBehavior Payload { get; private set; }

        public override int MaxConnectors => 2;
    }

    [System.Serializable]
    public class DeliveryProfile
    {
        public float radius;
        public int chainCount;

        public float tickRate;
        public float duration;

        public bool requiresLOS;
    }
    
    [System.Serializable]
    public class PayloadModifier
    {
        public float powerMultiplier = 1f;

        public List<ModifierEffect> effects;
    }
    
    [System.Serializable]
    public class ModifierEffect
    {
        public EffectType type;
        public float value;
    }
    
    public enum EffectType
    {
        AddPierce,
        AddSplit,
        AddDelay,
        AddRepeat,
        IncreaseAoE,
        ApplyStatus,
        ModifyTargeting,
        CreateTerrain,
        ApplyForce
    }
    
    [CreateAssetMenu(fileName = "StatusEffect", menuName = Const.ConfigRoot + "StatusEffect")]
    public class StatusEffect : ScriptableObject
    {
        public string statusName;

        public int maxStacks;
        public float duration;

        public List<StatusBehavior> behaviors;
    }
    
    [System.Serializable]
    public class StatusBehavior
    {
        public StatusTrigger trigger;
        public StatusEffectType effectType;

        public float value;
    }
    
    public enum StatusTrigger
    {
        OnTick,
        OnHit,
        OnMove,
        OnExpire
    }
    
    public enum StatusEffectType
    {
        Damage,
        Slow,
        Root,
        Spread,
        Detonate,
        Amplify
    }
}