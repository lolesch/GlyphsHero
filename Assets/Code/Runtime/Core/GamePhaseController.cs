using System;
using Code.Data.Enums;
using Code.Data.Items;
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

        [Header("Loot")]
        [SerializeField] private ItemConfig[] itemPool;
        [SerializeField, Min(1)] private int lootCount = 3;

        [field: SerializeField, ReadOnly] public GamePhase Current { get; private set; }

        public IPlayerData PlayerData { get; private set; }

        /// <summary>
        /// The player stash, broadcast once it exists. UI presenters listen for
        /// this instead of being referenced directly (GameLoop must not depend on
        /// UI). <see cref="CurrentStash"/> caches the value for late subscribers.
        /// </summary>
        public static event Action<ITetrisContainer> StashBound;
        public static ITetrisContainer CurrentStash { get; private set; }

        private IGamePhase _placementPhase;
        private IGamePhase _combatPhase;
        private IGamePhase _lootPhase;
        
        [SerializeField] private EncounterConfig currentEncounter;
        
        private void Awake()
        {
            PlayerData = new PlayerData(stashSize, currentEncounter);
            
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

            _lootPhase = new LootPhase(
                PlayerData,
                itemPool,
                lootCount,
                continueAfterLootButton,
                () => TransitionTo(GamePhase.Placement));

            gameOverButton.onClick.AddListener(
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }
        
        public void LoadMap(EncounterConfig encounterData)
        {
            _registry.ClearEnemies();
            pawnFactory.SpawnEnemies(encounterData);
            pawnFactory.SpawnAllys(encounterData);
        }

        private void Start()
        {
            confirmPlacementButton.gameObject.SetActive(false);
            continueAfterLootButton.gameObject.SetActive(false);
            gameOverButton.gameObject.SetActive(false);

            LoadMap(PlayerData.currentEncounter);
            AddItems();

            CurrentStash = PlayerData.Stash;
            StashBound?.Invoke(CurrentStash);

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

        [ContextMenu("AddItems")]
        private void AddItems()
        {
            foreach (var config in itemPool)
            {
                PlayerData.Stash.TryAdd(ItemFactory.Create(config));
                PlayerData.Stash.TryAdd(ItemFactory.Create(config));
            }
        }
    }

    public interface IGamePhase
    {
        void Enter();
        void Exit();
    }
}