using Code.Runtime.Core.Combat;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Locks the combat-end rule: a wiped enemy team is a player victory, a wiped player
    /// team is a defeat, and combat continues while both teams have a unit. This is the
    /// pure seam behind <see cref="CombatCoordinator"/> wipe detection (which is scene-coupled).
    /// </summary>
    [TestFixture]
    public sealed class CombatOutcomeResolverTests
    {
        [Test]
        public void EnemiesRemain_PlayerRemains_CombatContinues()
        {
            CombatOutcomeResolver.Resolve(playerCount: 1, enemyCount: 1).Should().BeNull();
        }

        [Test]
        public void AllEnemiesDead_IsPlayerVictory()
        {
            CombatOutcomeResolver.Resolve(playerCount: 1, enemyCount: 0)
                .Should().Be(CombatOutcome.PlayerVictory);
        }

        [Test]
        public void AllPlayersDead_IsPlayerDefeat()
        {
            CombatOutcomeResolver.Resolve(playerCount: 0, enemyCount: 1)
                .Should().Be(CombatOutcome.PlayerDefeat);
        }

        [Test]
        public void BothEmpty_PrefersVictory()
        {
            // Only one side can empty per single death, so a mutual-empty read defaults to
            // victory rather than punishing the player. Documents that assumption.
            CombatOutcomeResolver.Resolve(playerCount: 0, enemyCount: 0)
                .Should().Be(CombatOutcome.PlayerVictory);
        }
    }
}
