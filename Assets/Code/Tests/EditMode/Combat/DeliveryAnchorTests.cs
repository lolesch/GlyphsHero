using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Extensions;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the pure Anchor rule (ADR-0004 §3): where a delivery's geometry centres, now an axis of
    /// its own — independent of Affinity. <see cref="Anchor.Origin"/> centres on the firing pawn for
    /// every affinity (the decoupling that replaced the v1 "Self ⇒ self-anchored" rule); the default
    /// <see cref="Anchor.Target"/> centres on the chosen target.
    /// </summary>
    [TestFixture]
    public sealed class DeliveryAnchorTests
    {
        private static readonly Hex Origin = new(0, 0);
        private static readonly Hex Target = new(3, 0);

        [Test]
        public void Resolve_IsTarget_ForAnchorTarget()
        {
            DeliveryAnchor.Resolve(Origin, Target, Anchor.Target).Should().Be(Target);
        }

        [Test]
        public void Resolve_IsOrigin_ForAnchorOrigin()
        {
            DeliveryAnchor.Resolve(Origin, Target, Anchor.Origin).Should().Be(Origin);
        }

        [Test]
        public void Resolve_IsIndependentOfAffinity()
        {
            // The decoupling lock: anchor is chosen solely by the Anchor axis. The same Anchor value
            // yields the same hex no matter the affinity, so anchor-Origin works for Hostile (nova) and
            // Friendly (heal-around-me) alike, not only for the old self-coupled Self case.
            DeliveryAnchor.Resolve(Origin, Target, Anchor.Origin).Should().Be(Origin);
            DeliveryAnchor.Resolve(Origin, Target, Anchor.Target).Should().Be(Target);
        }
    }
}
