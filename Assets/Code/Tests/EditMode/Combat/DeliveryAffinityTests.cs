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
            DeliveryAffinity.TargetsCasterSide(Affinity.Hostile).Should().BeFalse();
            DeliveryAffinity.TargetsCasterSide(Affinity.Friendly).Should().BeTrue();
            DeliveryAffinity.TargetsCasterSide(Affinity.Self).Should().BeTrue();
        }
    }
}
