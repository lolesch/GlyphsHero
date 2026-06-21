using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the fixed-tick contract behind combat resolution (ADR-0001, Decision 6): the clock
    /// advances on a discrete interval decoupled from frame time, runs 0..N ticks per Advance with
    /// an accumulator that carries the remainder, and is therefore frame-rate independent — the
    /// same total elapsed time yields the same tick count regardless of how it is chunked.
    /// </summary>
    [TestFixture]
    public sealed class CombatClockTests
    {
        private const float Interval = 0.1f; // 10 ticks/sec

        [Test]
        public void SubIntervalAdvance_FiresNoTick_ButAccumulates()
        {
            var ticks = 0;
            var clock = new CombatClock(Interval);
            clock.OnTick += () => ticks++;

            clock.Advance(Interval * 0.6f).Should().Be(0);
            ticks.Should().Be(0);

            // The leftover 0.6 carries: another 0.6 crosses the interval exactly once.
            clock.Advance(Interval * 0.6f).Should().Be(1);
            ticks.Should().Be(1);
        }

        [Test]
        public void Advance_FiresOneTickPerWholeInterval()
        {
            var ticks = 0;
            var clock = new CombatClock(Interval);
            clock.OnTick += () => ticks++;

            clock.Advance(Interval * 2.5f).Should().Be(2);
            ticks.Should().Be(2);
        }

        [Test]
        public void Advance_IsFrameRateIndependent()
        {
            // One big step vs many small steps over the same total time → same tick count.
            // Kept within the per-Advance clamp (frame-rate independence is a within-budget
            // property; the spiral-of-death clamp deliberately breaks it for pathological frames,
            // covered separately by Advance_ClampsRunawayTicks).
            var coarse = 0;
            var clockCoarse = new CombatClock(Interval);
            clockCoarse.OnTick += () => coarse++;
            clockCoarse.Advance(Interval * 5f);

            var fine = 0;
            var clockFine = new CombatClock(Interval);
            clockFine.OnTick += () => fine++;
            for (var i = 0; i < 50; i++)
                clockFine.Advance(Interval * 0.1f);

            fine.Should().Be(coarse);
            coarse.Should().Be(5);
        }

        [Test]
        public void Advance_ClampsRunawayTicks_AgainstSpiralOfDeath()
        {
            var ticks = 0;
            var clock = new CombatClock(Interval, maxTicksPerAdvance: 5);
            clock.OnTick += () => ticks++;

            // A 100-interval hitch must not fire 100 ticks in one frame.
            clock.Advance(Interval * 100f).Should().Be(5);
            ticks.Should().Be(5);
        }

        [Test]
        public void Reset_DropsAccumulatedRemainder()
        {
            var ticks = 0;
            var clock = new CombatClock(Interval);
            clock.OnTick += () => ticks++;

            clock.Advance(Interval * 0.9f);
            clock.Reset();
            clock.Advance(Interval * 0.9f).Should().Be(0); // remainder was cleared, so still short
            ticks.Should().Be(0);
        }

        [Test]
        public void TickInterval_IsExposed()
        {
            new CombatClock(Interval).TickInterval.Should().Be(Interval);
        }
    }
}
