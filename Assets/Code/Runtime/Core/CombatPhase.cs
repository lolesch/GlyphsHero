using System;
using Code.Runtime.Core.Combat;

namespace Code.Runtime.Core
{
    public sealed class CombatPhase : IGamePhase
    {
        private readonly ICombatCoordinator _coordinator;
        private readonly Action             _onVictory;
        private readonly Action             _onDefeat;

        public CombatPhase(ICombatCoordinator coordinator, Action onVictory, Action onDefeat)
        {
            _coordinator = coordinator;
            _onVictory   = onVictory;
            _onDefeat    = onDefeat;
        }

        public void Enter()
        {
            // Subscribe before starting so an encounter that begins with an empty side is caught.
            _coordinator.OnCombatEnded += HandleCombatEnded;
            _coordinator.StartCombat();
        }

        public void Exit()
        {
            _coordinator.OnCombatEnded -= HandleCombatEnded;
            _coordinator.StopCombat();
        }

        private void HandleCombatEnded(CombatOutcome outcome)
        {
            if (outcome == CombatOutcome.PlayerVictory)
                _onVictory();
            else
                _onDefeat();
        }
    }
}
