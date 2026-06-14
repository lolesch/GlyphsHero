using Code.Runtime.Core;
using Code.Runtime.Modules.Inventory;
using UnityEngine;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// Source-selection policy for an <see cref="InventoryView"/>: binds the
    /// renderer to the player stash. The stash is owned by
    /// <see cref="GamePhaseController"/> and delivered via a static event
    /// (UI must not reference GameLoop the other way around), with a cached
    /// current value so a panel enabled after the broadcast still binds.
    /// </summary>
    [RequireComponent(typeof(InventoryView))]
    public sealed class PlayerStashView : MonoBehaviour
    {
        [SerializeField] private InventoryView _view;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<InventoryView>();
        }

        private void OnEnable()
        {
            if (GamePhaseController.CurrentStash != null)
                Bind(GamePhaseController.CurrentStash);

            GamePhaseController.StashBound += Bind;
        }

        private void OnDisable() => GamePhaseController.StashBound -= Bind;

        public void Bind(ITetrisContainer stash) => _view.RefreshView(stash);
    }
}
