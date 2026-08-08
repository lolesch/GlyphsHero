using System;
using Code.Data.Enums;
using Code.Data.Pawns;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditMode.Statistics
{
    /// <summary>
    /// PawnStats stat-mod application. PawnStat.None is the "no passive stat" sentinel (the default
    /// for an unset pawnStatMod); applying/removing such a mod must be a harmless no-op, never a
    /// throw — otherwise an attachment authored without a passive crashes pawn spawn.
    /// </summary>
    [TestFixture]
    public sealed class PawnStatsTests
    {
        private static PawnStats NewStats() =>
            new(ScriptableObject.CreateInstance<PawnConfig>());

        private static PawnStatModifier NoneMod() =>
            new(PawnStat.None, new Modifier(50f, ModifierType.FlatAdd, Guid.NewGuid()));

        [Test]
        public void ApplyMod_NoneStat_IsNoOpAndDoesNotThrow()
        {
            var stats  = NewStats();
            var before = (float)stats.health;

            Action act = () => stats.ApplyMod(NoneMod());

            act.Should().NotThrow();
            ((float)stats.health).Should().Be(before);
        }

        [Test]
        public void RemoveMod_NoneStat_IsNoOpAndDoesNotThrow()
        {
            var stats = NewStats();

            Action act = () => stats.RemoveMod(NoneMod());

            act.Should().NotThrow();
        }

        // ── PreviewAffix: non-mutating owned-context Details math (tooltip issue #22) ──

        [Test]
        public void PreviewAffix_NoneStat_ReturnsZeroZeroAndDoesNotThrow()
        {
            var stats = NewStats();

            Action act = () => stats.PreviewAffix(NoneMod());

            act.Should().NotThrow();
            stats.PreviewAffix(NoneMod()).Should().Be((0f, 0f));
        }

        [Test]
        public void PreviewAffix_ModAlreadyLive_ReportsWithoutAndWithButLeavesItApplied()
        {
            // Mirrors a loose (unchained) attachment: OnUnchained already applied the affix.
            var stats = NewStats();
            var mod   = new PawnStatModifier(PawnStat.LifeMax, new Modifier(10f, ModifierType.FlatAdd, Guid.NewGuid()));
            stats.ApplyMod(mod);
            ((float)stats.health).Should().Be(110f);

            var (before, after) = stats.PreviewAffix(mod);

            before.Should().Be(100f);
            after.Should().Be(110f);
            ((float)stats.health).Should().Be(110f); // untouched by the preview
        }

        [Test]
        public void PreviewAffix_ModNotYetApplied_ReportsWithoutAndWithAndNeverMutatesTheLiveStat()
        {
            // Mirrors a chained attachment: its affix is currently suppressed (never ApplyMod'd).
            var stats = NewStats();
            var mod   = new PawnStatModifier(PawnStat.LifeMax, new Modifier(10f, ModifierType.FlatAdd, Guid.NewGuid()));

            var (before, after) = stats.PreviewAffix(mod);

            before.Should().Be(100f);
            after.Should().Be(110f);
            ((float)stats.health).Should().Be(100f); // preview never applied it
        }
    }
}
