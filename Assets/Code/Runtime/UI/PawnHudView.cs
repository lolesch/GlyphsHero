using Code.Runtime.Modules.Statistics;
using Code.Runtime.Pawns;
using Code.Runtime.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.UI
{
    /// <summary>
    /// Selected-pawn HUD (pawn-ui spec §Decision 3). Inspects whichever pawn the player has
    /// click-selected via <see cref="HexSelectionHandler.OnPawnSelected"/> (any team, enemies
    /// included) and shows its identity (icon + name), health/mana pools (fill bar + numeric
    /// current/max) and the four secondary stats (healthRegen, manaRegen, movementSpeed, range).
    ///
    /// Fully live: the pools drive off <see cref="Resource.OnCurrentChanged"/> — which also forwards
    /// max changes, because <see cref="Resource"/> re-runs <c>SetCurrentTo</c> whenever MaxValue
    /// changes, so a chained LifeMax/ManaMax item updates both the fill and the numbers without a
    /// second subscription. The four plain stats drive off <see cref="Stat.OnTotalChanged"/> (#11).
    /// The previous pawn's subscriptions are dropped on every selection switch and in
    /// <see cref="OnDestroy"/>.
    ///
    /// Separate panel from the inventory view — it shares only the selection source. Reads an
    /// <see cref="IPawn"/> only, so the UI → Pawns layering holds. IMPORTANT: this component must sit
    /// on an always-active object and toggle the child <see cref="_root"/>; disabling its own
    /// GameObject would run <see cref="OnDisable"/> and stop it hearing later selections.
    /// </summary>
    public sealed class PawnHudView : MonoBehaviour
    {
        [Header( "Visibility" )]
        [SerializeField] private GameObject _root; // panel content; hidden until the first selection

        [Header( "Identity" )]
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _name;

        [Header( "Pools" )]
        [SerializeField] private PawnResourceView _healthBar;
        [SerializeField] private TMP_Text         _healthText;
        [SerializeField] private PawnResourceView _manaBar;
        [SerializeField] private TMP_Text         _manaText;

        [Header( "Secondary stats" )]
        [SerializeField] private TMP_Text _healthRegen;
        [SerializeField] private TMP_Text _manaRegen;
        [SerializeField] private TMP_Text _movementSpeed;
        [SerializeField] private TMP_Text _range;

        private IPawnStats _bound;

        private void Awake()
        {
            if( _root != null )
                _root.SetActive( false );
        }

        private void OnEnable()  => HexSelectionHandler.OnPawnSelected += HandleSelection;
        private void OnDisable() => HexSelectionHandler.OnPawnSelected -= HandleSelection;

        // The bars own their own OnCurrentChanged lifecycle; here we only drop this view's own
        // text/stat subscriptions on the previously bound pawn.
        private void OnDestroy() => Unbind();

        private void HandleSelection( IPawn pawn )
        {
            Unbind();

            if( pawn == null || pawn.Stats == null )
                return;

            Bind( pawn );
        }

        private void Bind( IPawn pawn )
        {
            _bound = pawn.Stats;

            if( _icon != null ) _icon.sprite = pawn.Icon;
            if( _name != null ) _name.text   = pawn.DisplayName;

            // The fill bars self-manage their subscribe/repaint/rebind-unsubscribe (PawnResourceView),
            // so switching pawns just re-points them; this view never touches their event.
            if( _healthBar != null ) _healthBar.SetPawn( _bound.health );
            if( _manaBar   != null ) _manaBar.SetPawn( _bound.mana );

            _bound.health.OnCurrentChanged      += OnHealthChanged;
            _bound.mana.OnCurrentChanged        += OnManaChanged;
            _bound.healthRegen.OnTotalChanged   += OnHealthRegenChanged;
            _bound.manaRegen.OnTotalChanged     += OnManaRegenChanged;
            _bound.movementSpeed.OnTotalChanged += OnMovementSpeedChanged;
            _bound.range.OnTotalChanged         += OnRangeChanged;

            Paint();

            if( _root != null )
                _root.SetActive( true );
        }

        private void Unbind()
        {
            if( _bound == null )
                return;

            _bound.health.OnCurrentChanged      -= OnHealthChanged;
            _bound.mana.OnCurrentChanged        -= OnManaChanged;
            _bound.healthRegen.OnTotalChanged   -= OnHealthRegenChanged;
            _bound.manaRegen.OnTotalChanged     -= OnManaRegenChanged;
            _bound.movementSpeed.OnTotalChanged -= OnMovementSpeedChanged;
            _bound.range.OnTotalChanged         -= OnRangeChanged;

            _bound = null;
        }

        // Pool text reads the total carried on OnCurrentChanged, so a max change (a chained item
        // raising LifeMax/ManaMax, which re-fires OnCurrentChanged via Resource) keeps the numbers
        // current without a separate OnTotalChanged subscription on the pool.
        private void OnHealthChanged( float prev, float curr, float max ) => SetPool( _healthText, curr, max );
        private void OnManaChanged( float prev, float curr, float max )   => SetPool( _manaText, curr, max );

        private void OnHealthRegenChanged( float value )   => SetStat( _healthRegen, value );
        private void OnManaRegenChanged( float value )     => SetStat( _manaRegen, value );
        private void OnMovementSpeedChanged( float value ) => SetStat( _movementSpeed, value );
        private void OnRangeChanged( float value )         => SetStat( _range, value );

        // Initial paint on bind — the events only fire on later changes, so without this the panel
        // would show stale text until the first damage/regen/chain tick (same reasoning as
        // PawnResourceView.Paint).
        private void Paint()
        {
            SetPool( _healthText, _bound.health.CurrentValue, _bound.health );
            SetPool( _manaText,   _bound.mana.CurrentValue,   _bound.mana );
            SetStat( _healthRegen,   _bound.healthRegen );
            SetStat( _manaRegen,     _bound.manaRegen );
            SetStat( _movementSpeed, _bound.movementSpeed );
            SetStat( _range,         _bound.range );
        }

        private static void SetPool( TMP_Text text, float current, float max )
        {
            if( text != null )
                text.text = $"{current:0} / {max:0}";
        }

        private static void SetStat( TMP_Text text, float value )
        {
            if( text != null )
                text.text = $"{value:0.###}";
        }
    }
}
