using System;
using Code.Runtime.Pawns;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.Pawns
{
    /// <summary>
    /// Locks the view-side movement glide (ADR-0001 lerp polish): a pure step that eases a world
    /// position from <c>from</c> to <c>to</c> over a fixed duration, tick-locked by the caller. The
    /// easing shape is injected so the endpoints/clamping can be asserted independently of any curve.
    /// </summary>
    [TestFixture]
    public sealed class MoveInterpolatorTests
    {
        private static readonly Vector3 A = new(0f, 0f, 0f);
        private static readonly Vector3 B = new(4f, 0f, 0f);
        private static readonly Func<float, float> Linear = t => t;

        private static void ShouldBeAt(Vector3 actual, Vector3 expected) =>
            Vector3.Distance(actual, expected).Should().BeLessThan(1e-4f,
                $"expected {expected} but was {actual}");

        [Test]
        public void AdvanceFullDuration_ArrivesExactlyAtTarget_AndStops()
        {
            var move = new MoveInterpolator();
            move.Begin(A, B, 1f);

            move.Advance(1f, Linear);

            ShouldBeAt(move.Position, B);
            move.IsMoving.Should().BeFalse();
        }

        [Test]
        public void AdvanceHalfway_LinearEase_IsAtMidpoint()
        {
            var move = new MoveInterpolator();
            move.Begin(A, B, 1f);

            move.Advance(0.5f, Linear);

            ShouldBeAt(move.Position, new Vector3(2f, 0f, 0f));
            move.IsMoving.Should().BeTrue();
        }

        [Test]
        public void AdvancePastDuration_ClampsToTarget_NoOvershoot()
        {
            var move = new MoveInterpolator();
            move.Begin(A, B, 1f);

            move.Advance(5f, Linear);

            ShouldBeAt(move.Position, B);
            move.IsMoving.Should().BeFalse();
        }

        [Test]
        public void ZeroDuration_SnapsToTarget_AndIsNotMoving()
        {
            var move = new MoveInterpolator();
            move.Begin(A, B, 0f);

            move.IsMoving.Should().BeFalse();
            ShouldBeAt(move.Position, B);
        }

        [Test]
        public void SameFromAndTo_IsNotMoving()
        {
            var move = new MoveInterpolator();
            move.Begin(A, A, 1f);

            move.IsMoving.Should().BeFalse();
            ShouldBeAt(move.Position, A);
        }

        // Mutation guard: proves the injected easing actually reshapes progress rather than the
        // interpolator using raw t. With ease t->t^2 at t=0.5, eased progress is 0.25, so the
        // position is a quarter of the way (1.0), not the linear midpoint (2.0).
        [Test]
        public void Advance_AppliesEasingShape_NotRawProgress()
        {
            var move = new MoveInterpolator();
            move.Begin(A, B, 1f);

            move.Advance(0.5f, t => t * t);

            ShouldBeAt(move.Position, new Vector3(1f, 0f, 0f));
        }
    }
}
