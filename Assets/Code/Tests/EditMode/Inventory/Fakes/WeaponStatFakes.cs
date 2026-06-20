using System;
using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;

namespace Code.Tests.EditMode.Inventory.Fakes
{
    /// <summary>
    /// Connector-free fakes for WeaponStatResolver tests. Unlike the ChainResolver fakes, these
    /// expose fully configurable base stats and modifier values, so a test can prove exactly which
    /// modifier lands on which stat — e.g. that a shifter's outputMod (not its inputMod) drives the
    /// output stat. Stat resolution takes a flat item list, so no grid connectors are needed.
    /// </summary>
    internal static class Mods
    {
        public static Modifier Flat(float value)    => new Modifier(value, ModifierType.FlatAdd,    Guid.NewGuid());
        public static Modifier Percent(float value) => new Modifier(value, ModifierType.PercentAdd, Guid.NewGuid());

        public static WeaponInputModifier  Input(WeaponInputStat stat, Modifier modifier)   => new WeaponInputModifier(stat, modifier);
        public static WeaponOutputModifier Output(WeaponOutputStat stat, Modifier modifier) => new WeaponOutputModifier(stat, modifier);
    }

    internal sealed class StatWeapon : FakeItem, IWeaponItem
    {
        public StatWeapon(float damage = 0f, float attackSpeed = 0f, float resourceCost = 0f,
            float resourceGenOnHit = 0f, string name = "Weapon") : base(name)
        {
            Damage           = new MutableFloat(damage);
            AttackSpeed      = new MutableFloat(attackSpeed);
            ResourceCost     = new MutableFloat(resourceCost);
            ResourceGenOnHit = new MutableFloat(resourceGenOnHit);
        }

        public MutableFloat    Damage           { get; }
        public MutableFloat    AttackSpeed      { get; }
        public MutableFloat    ResourceCost     { get; }
        public MutableFloat    ResourceGenOnHit { get; }
        public PayloadBehavior Payload          => null;
    }

    internal sealed class StatAmplifier : FakeItem, IAmplifierItem
    {
        public StatAmplifier(WeaponOutputModifier outputMod, string name = "Amp") : base(name)
            => this.outputMod = outputMod;

        public WeaponOutputModifier outputMod { get; }
    }

    internal sealed class StatShifter : FakeItem, IShifterItem
    {
        public StatShifter(WeaponInputModifier inputMod, WeaponOutputModifier outputMod, string name = "Shifter")
            : base(name)
        {
            this.inputMod  = inputMod;
            this.outputMod = outputMod;
        }

        public WeaponInputModifier  inputMod  { get; }
        public WeaponOutputModifier outputMod { get; }
    }

    internal sealed class StatReactor : FakeItem, IReactorItem
    {
        public StatReactor(WeaponInputModifier inputMod, ReactorType reactorType = ReactorType.OnSelfHit,
            string name = "Reactor") : base(name)
        {
            this.inputMod = inputMod;
            ReactorType   = reactorType;
        }

        public ReactorType         ReactorType { get; }
        public WeaponInputModifier inputMod    { get; }
    }
}
