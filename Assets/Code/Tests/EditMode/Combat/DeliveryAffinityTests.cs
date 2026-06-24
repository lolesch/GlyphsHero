using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Extensions;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure Affinity rules (ADR-0004 §3): whose side a delivery resolves against, and the
    /// v1 anchor coupling (Self is self-anchored, Hostile/Friendly anchor on the target). This is the
    /// decision that replaced the old <c>DeliveryPattern.Self</c> flag — it keeps the deliberate
    /// self-hurt build-around working (a Self delivery centres on, and hits, the firing pawn).
    /// </summary>
    [TestFixture]
    public sealed class DeliveryAffinityTests
    {
        private static readonly Hex Origin = new(0, 0);
        private static readonly Hex Target = new(3, 0);

        [Test]
        public void TargetsCasterSide_OnlyForFriendlyAndSelf()
        {
            DeliveryAffinity.TargetsCasterSide(Affinity.Hostile).Should().BeFalse();
            DeliveryAffinity.TargetsCasterSide(Affinity.Friendly).Should().BeTrue();
            DeliveryAffinity.TargetsCasterSide(Affinity.Self).Should().BeTrue();
        }

        [Test]
        public void Anchor_IsOrigin_ForSelf()
        {
            DeliveryAffinity.Anchor(Origin, Target, Affinity.Self).Should().Be(Origin);
        }

        [Test]
        public void Anchor_IsTarget_ForHostileAndFriendly()
        {
            DeliveryAffinity.Anchor(Origin, Target, Affinity.Hostile).Should().Be(Target);
            DeliveryAffinity.Anchor(Origin, Target, Affinity.Friendly).Should().Be(Target);
        }
    }
}
