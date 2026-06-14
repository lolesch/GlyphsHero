namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Pure combat-end rule, isolated from the scene-coupled <see cref="CombatCoordinator"/>
    /// so it can be unit-tested. A wiped enemy team is a victory; a wiped player team is a
    /// defeat; otherwise combat continues.
    /// </summary>
    public static class CombatOutcomeResolver
    {
        /// <summary>Returns the outcome, or <c>null</c> while combat should continue.</summary>
        public static CombatOutcome? Resolve(int playerCount, int enemyCount)
        {
            // Only one side can empty per single death, so checking victory first is safe
            // and, in the defensive mutual-empty case, avoids punishing the player.
            if (enemyCount == 0) return CombatOutcome.PlayerVictory;
            if (playerCount == 0) return CombatOutcome.PlayerDefeat;
            return null;
        }
    }
}
