using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure Affinity rule (ADR-0004 §3): whose side a delivery resolves against. This is the
    /// decision that replaced the old <c>DeliveryPattern.Self</c> flag. Where the geometry centres is
    /// now the independent Anchor axis — see <see cref="DeliveryAnchorTests"/>.
    /// </summary>
    [TestFixture]
    public sealed class DeliveryAffinityTests
    {
        [Test]
        public void TargetsCasterSide_OnlyForFriendlyAndSelf()
        {
            DeliveryAffinity.TargetsCasterSide(Affinity.None).Should().BeFalse();
            DeliveryAffinity.TargetsCasterSide(Affinity.Hostile).Should().BeFalse();
            DeliveryAffinity.TargetsCasterSide(Affinity.Friendly).Should().BeTrue();
            DeliveryAffinity.TargetsCasterSide(Affinity.Self).Should().BeTrue();
        }

        [Test]
        public void TargetsCasterSide_TrueForAnyCompositeContainingFriendlyOrSelf()
        {
            DeliveryAffinity.TargetsCasterSide(Affinity.Friendly | Affinity.Self).Should().BeTrue();
            DeliveryAffinity.TargetsCasterSide(Affinity.Hostile | Affinity.Self).Should().BeTrue();
        }

        [Test]
        public void TargetsEnemySide_OnlyForHostile()
        {
            DeliveryAffinity.TargetsEnemySide(Affinity.None).Should().BeFalse();
            DeliveryAffinity.TargetsEnemySide(Affinity.Hostile).Should().BeTrue();
            DeliveryAffinity.TargetsEnemySide(Affinity.Friendly).Should().BeFalse();
            DeliveryAffinity.TargetsEnemySide(Affinity.Self).Should().BeFalse();
            DeliveryAffinity.TargetsEnemySide(Affinity.Hostile | Affinity.Self).Should().BeTrue();
        }
    }
}
