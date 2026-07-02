using Code.Runtime.Core;
using Code.Runtime.Pawns;
using UnityEngine;

namespace Code.Runtime.UI.Inventory
{
    /// <summary>
    /// Source-selection policy for an <see cref="InventoryView"/>: drives the
    /// renderer with the currently selected pawn's inventory. The view itself
    /// knows nothing about pawns or selection.
    /// </summary>
    [RequireComponent(typeof(InventoryView))]
    public sealed class PawnInventoryView : MonoBehaviour
    {
        [SerializeField] private InventoryView _view;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<InventoryView>();
        }

        private void OnEnable()  => HexSelectionHandler.OnPawnSelected += Show;
        private void OnDisable() => HexSelectionHandler.OnPawnSelected -= Show;

        public void Show(IPawn pawn) => _view.RefreshView(pawn.Inventory);
    }
}
