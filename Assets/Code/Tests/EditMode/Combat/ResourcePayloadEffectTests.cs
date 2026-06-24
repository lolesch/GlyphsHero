using Code.Data.Enums;
using Code.Data.Items.Weapon;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the leech math on ResourcePayloadEffect.ComputeGain — the pure calculation that
    /// replaced the old WeaponConfig.ResourceGenOnHit field (ADR-0005 §3). Pool selection and
    /// IncreaseCurrent live in PawnCombatController.ExecuteEffect; this tests the math only.
    /// </summary>
    [TestFixture]
    public sealed class ResourcePayloadEffectTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void ComputeGain_LeechPercent_ReturnsPercentOfDamage()
        {
            var effect = new ResourcePayloadEffect(ResourceType.Mana, percentOfDamage: 50f);

            var gain = effect.ComputeGain(damageDealt: 10f); // 50 % of 10 = 5

            gain.Should().BeApproximately(5f, Tolerance);
        }

        [Test]
        public void ComputeGain_FlatAmount_ReturnsFlatRegardlessOfDamage()
        {
            var effect = new ResourcePayloadEffect(ResourceType.Mana, flatAmount: 3f);

            var gain = effect.ComputeGain(damageDealt: 999f); // flat ignores damage

            gain.Should().BeApproximately(3f, Tolerance);
        }

        [Test]
        public void ComputeGain_PercentTakesPriorityOverFlat_WhenBothSet()
        {
            var effect = new ResourcePayloadEffect(ResourceType.Mana, percentOfDamage: 50f, flatAmount: 99f);

            var gain = effect.ComputeGain(damageDealt: 10f); // percent wins: 50 % of 10 = 5, not 99

            gain.Should().BeApproximately(5f, Tolerance);
        }
    }
}
