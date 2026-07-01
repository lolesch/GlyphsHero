using Code.Runtime.Pawns;
using UnityEngine;

namespace Code.Runtime.UI
{
    /// <summary>
    /// Grid-following status bar living on a world-space canvas child of <c>Pawn.prefab</c>.
    /// Self-binds its two <see cref="PawnResourceView"/> bars (health, mana) to the parent pawn's
    /// stats in <see cref="Start"/> — which runs after <c>PawnFactory.SpawnPawn</c>'s synchronous
    /// spawn, so <c>Pawn.Stats</c> is ready. Respects the UI → Pawns layering: this reads a Pawn,
    /// Pawn/PawnFactory never reference UI.
    /// </summary>
    public sealed class PawnStatusBar : MonoBehaviour
    {
        [SerializeField] private PawnResourceView healthBar;
        [SerializeField] private PawnResourceView manaBar;

        private void Start()
        {
            var pawn = GetComponentInParent<Pawn>();
            if( pawn == null )
            {
                Debug.LogWarning( "PawnStatusBar found no Pawn in parents; bars will stay empty.", this );
                return;
            }

            var stats = pawn.Stats;
            if( stats == null )
            {
                Debug.LogWarning( "PawnStatusBar: parent Pawn has no Stats yet; bars will stay empty.", this );
                return;
            }

            if( healthBar != null )
                healthBar.SetPawn( stats.health );

            if( manaBar != null )
                manaBar.SetPawn( stats.mana );
        }
    }
}
