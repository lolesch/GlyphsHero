using System;
using System.Collections.Generic;
using Code.Data.Enums;
using Code.Data.Pawns;
using Code.Runtime.Core.Combat;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Pawns;
using Code.Runtime.UI.Inventory;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Runtime.Core
{
    /// <summary>
    /// Owns the game loop state machine.
    /// Drives transitions between Placement → Combat → Loot → Placement.
    /// All per-phase logic lives in the phase classes; this is the coordinator.
    /// </summary>
    public sealed class GamePhaseController : MonoBehaviour
    {
        private readonly PawnRegistry _registry = new();

        [Header("Combat")]
        [SerializeField] private CombatCoordinator combatCoordinator;
        [SerializeField] private HexSelectionHandler selectionHandler;
        [SerializeField] private PawnFactory pawnFactory;

        [Header("Stash")]
        [SerializeField] private Vector2Int stashSize = new(8, 6);

        [Header("UI")]
        [SerializeField] private Button confirmPlacementButton;
        [SerializeField] private Button continueAfterLootButton;
        [SerializeField] private Button gameOverButton;

        [field: SerializeField, ReadOnly] public GamePhase Current { get; private set; }

        public IPlayerData PlayerData { get; private set; }

        /// <summary>
        /// The player stash, broadcast once it exists. UI presenters listen for
        /// this instead of being referenced directly (GameLoop must not depend on
        /// UI). <see cref="CurrentStash"/> caches the value for late subscribers.
        /// </summary>
        public static event Action<ITetrisContainer> StashBound;
        public static ITetrisContainer CurrentStash { get; private set; }

        /// <summary>Same one-way pattern as StashBound, for the Loot phase's offer/pick surface.</summary>
        public static event Action<ILootOffer> LootOfferBound;
        public static ILootOffer CurrentLootOffer { get; private set; }

        private IGamePhase _placementPhase;
        private IGamePhase _combatPhase;
        private IGamePhase _lootPhase;

        // Hand-authored escalating sequence, played in order. Index 0 is the tutorial fight; the
        // sequence clamps on its last entry once exhausted (no "victory" end-state yet — noted as
        // a gap, out of scope for this slice).
        [SerializeField] private List<EncounterConfig> encounters = new();
        private int _encounterIndex;
        private EncounterConfig CurrentEncounter => encounters[Mathf.Clamp(_encounterIndex, 0, encounters.Count - 1)];

        private void Awake()
        {
            if (encounters.Count == 0)
                Debug.LogError("[GameLoop] No EncounterConfig assigned to Encounters — assign at least one in the Inspector.", this);

            PlayerData = new PlayerData(stashSize, encounters.Count > 0 ? encounters[0] : null);

            combatCoordinator.Initialize(_registry);
            selectionHandler.Initialize(_registry);
            pawnFactory.Initialize(_registry);

            _placementPhase = new PlacementPhase(
                _registry.playerPawns,
                confirmPlacementButton,
                () => TransitionTo(GamePhase.Combat));

            _combatPhase = new CombatPhase(
                combatCoordinator,
                () => TransitionTo(GamePhase.Loot), // victory
                OnPlayerDefeated);                  // defeat → Game Over

            var lootPhase = new LootPhase(
                PlayerData,
                () => CurrentEncounter.scriptedLoot,
                () => CurrentEncounter.lootPickCount,
                continueAfterLootButton,
                OnLootContinue);
            _lootPhase       = lootPhase;
            CurrentLootOffer = lootPhase;

            gameOverButton.onClick.AddListener(
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        public void LoadMap(EncounterConfig encounterData)
        {
            _registry.ClearEnemies();
            pawnFactory.SpawnEnemies(encounterData);
            pawnFactory.SpawnAllys(encounterData); // additive: encounterData.players lists only newly-introduced pawns
        }

        // Advances to the next scripted encounter (clamped on the last one) and loads it before
        // returning to Placement, so the player gears up against next fight's enemies, not the one
        // they just won.
        private void OnLootContinue()
        {
            _encounterIndex = Mathf.Min(_encounterIndex + 1, encounters.Count - 1);
            PlayerData.currentEncounter = CurrentEncounter;
            LoadMap(CurrentEncounter);
            TransitionTo(GamePhase.Placement);
        }

        private void Start()
        {
            if (encounters.Count == 0)
                return; // Awake already logged the error; nothing to load.

            confirmPlacementButton.gameObject.SetActive(false);
            continueAfterLootButton.gameObject.SetActive(false);
            gameOverButton.gameObject.SetActive(false);

            LoadMap(CurrentEncounter);

            CurrentStash = PlayerData.Stash;
            StashBound?.Invoke(CurrentStash);
            LootOfferBound?.Invoke(CurrentLootOffer);

            TransitionTo(GamePhase.Placement);
        }

        private void TransitionTo(GamePhase next)
        {
            GetPhase(Current)?.Exit();
            Current = next;
            GetPhase(Current)?.Enter();
        }

        private void OnPlayerDefeated()
        {
            // GetPhase(GameOver) is null, so this cleanly Exits combat and Enters nothing.
            TransitionTo(GamePhase.GameOver);
            Debug.Log("[GameLoop] Game Over");
            gameOverButton.gameObject.SetActive(true); // restart (reloads the scene)
        }

        private IGamePhase GetPhase(GamePhase phase) => phase switch
        {
            GamePhase.Placement => _placementPhase,
            GamePhase.Combat    => _combatPhase,
            GamePhase.Loot      => _lootPhase,
            _                   => null,
        };
    }

    public interface IGamePhase
    {
        void Enter();
        void Exit();
    }
}