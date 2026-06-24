using System.Collections.Generic;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    /// <summary>
    /// The one rule for turning a weapon plus its chained modifiers into computed
    /// <see cref="WeaponStats"/>. Pure: it seeds throwaway MutableFloats from the weapon's base
    /// values and reads back the totals, so the weapon's live stats are never touched. Combat and
    /// the tooltip both call this instead of mutating the weapon and reverting.
    /// </summary>
    public static class WeaponStatResolver
    {
        /// <summary>
        /// Resolves the stats for a chain's firing. The chain's <see cref="IItemChain.Root"/> is a
        /// contributor too: a shifter/reactor root carries mods that ChainResolver keeps out of
        /// <see cref="IItemChain.Modifiers"/>, so both are folded in here.
        /// </summary>
        public static WeaponStats Resolve(IItemChain chain)
        {
            var weapon = chain.Weapon;
            return weapon == null ? default : Resolve(weapon, Contributors(chain));
        }

        private static IEnumerable<ITetrisItem> Contributors(IItemChain chain)
        {
            if (chain.Root != null) yield return chain.Root;
            foreach (var modifier in chain.Modifiers) yield return modifier;
        }

        public static WeaponStats Resolve(IWeaponItem weapon, IEnumerable<ITetrisItem> contributors)
        {
            var damage       = new MutableFloat(weapon.Damage);
            var attackSpeed  = new MutableFloat(weapon.AttackSpeed);
            var resourceCost = new MutableFloat(weapon.ResourceCost);

            // The type axes are seeded from the weapon and reclassified by any chained Converter
            // (ADR-0004 §1, ADR-0005 §2) — kind, never amount. Replace, last-wins.
            var delivery     = weapon.Delivery;
            var affinity     = weapon.Affinity;
            var anchor       = weapon.Anchor;
            var costResource = weapon.CostResource;

            void ApplyOutput(WeaponOutputModifier mod)
            {
                switch (mod.stat)
                {
                    case WeaponOutputStat.Damage: damage.AddModifier(mod.modifier); break;
                }
            }

            // ProcChance has no WeaponStats field — drop it silently so an exotic shifter never crashes.
            void ApplyInput(WeaponInputModifier mod)
            {
                switch (mod.stat)
                {
                    case WeaponInputStat.AttackSpeed: attackSpeed.AddModifier(mod.modifier);  break;
                    case WeaponInputStat.ManaCost:    resourceCost.AddModifier(mod.modifier); break;
                }
            }

            // A Converter changes the kind on one axis only — the matching To* value replaces the seed.
            void ApplyConversion(IConverterItem converter)
            {
                switch (converter.Axis)
                {
                    case ConverterAxis.Delivery: delivery     = converter.ToDelivery; break;
                    case ConverterAxis.Affinity: affinity     = converter.ToAffinity; break;
                    case ConverterAxis.Anchor:   anchor       = converter.ToAnchor;   break;
                    case ConverterAxis.Resource: costResource = converter.ToResource; break;
                }
            }

            foreach (var item in contributors)
            {
                switch (item)
                {
                    case IAmplifierItem amp:
                        ApplyOutput(amp.outputMod);
                        break;
                    case IShifterItem shifter:
                        ApplyInput(shifter.inputMod);
                        ApplyOutput(shifter.outputMod);
                        break;
                    case IReactorItem reactor:
                        ApplyInput(reactor.inputMod);
                        break;
                    case IConverterItem converter:
                        ApplyConversion(converter);
                        break;
                }
            }

            return new WeaponStats(damage, attackSpeed, resourceCost, costResource, delivery, affinity, anchor);
        }
    }
}
