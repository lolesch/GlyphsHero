using System;
using Code.Data.Enums;
using Code.Data.Pawns;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using NaughtyAttributes;
using Submodules.Utility.Attributes;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.Pawns
{
    public sealed class Pawn : MonoBehaviour, IPawn
    {
        [SerializeField, ReadOnly, PreviewIcon] private Sprite _icon;
        // TODO: move pawn effect into config!
        [SerializeField] private PawnEffect _pawnEffects;
        [SerializeField] private float _moveLerpSpeed = 8f;
        private Grid _grid;

        //[field: SerializeField] public PawnConfig        Config        { get; private set; }
        [field: SerializeField, ReadOnly] public PawnTeam          Team          { get; private set; }
        [field: SerializeField, ReadOnly] public Hex               HexPosition   { get; private set; }
        [field: SerializeField, ReadOnly] public TerrainCostConfig    MovementCosts { get; private set; }
        
        public IPawnStats       Stats         { get; private set; }
        public ITetrisContainer Inventory     { get; private set; }
        public IPawnEffect      PawnEffects   { get; private set; }

        // Owns the attachment chain-state seam for this pawn's whole lifetime (placement included):
        // a loose attachment grants its passive pawn-stat affix, lost once it joins a weapon chain.
        private ChainStateController _chainState;
       
        public event Action OnDefeated;

        public  void SpawnPawn(PawnConfig config, PawnTeam team, Hex hex, Grid grid)
        {
            if (!config)
            {
                Debug.LogError("Missing Config to draw from");
                return;
            }

            _grid         = grid;
            _icon         = config.icon;
            Stats         = new PawnStats(config);
            Inventory     = new TetrisContainer(new Vector2Int(6, 3));
            PawnEffects   = _pawnEffects;
            MovementCosts = config.movementCosts;
            
            if (config.starterWeapon != null)
                Inventory.TryAdd(ItemFactory.Create(config.starterWeapon));
            else
                Debug.LogWarning($"{gameObject.name} has no StarterWeapon assigned in PawnConfig.", this);

            // Bootstrap after the starter weapon is in: it applies loose attachments' passive stats
            // and keeps them in sync as the player chains/unchains items in any phase.
            _chainState = new ChainStateController(Inventory, Stats);

            Stats.health.OnDepleted += DespawnPawn;
            
            Team = team;
            MoveTo(hex);
            if (_grid != null)
                transform.position = hex.ToWorld(_grid);

            gameObject.SetActive(true);
        }

        private void DespawnPawn()
        {
            Debug.Log($"{gameObject.name} has been defeated!");
            OnDefeated?.Invoke();
            
            gameObject.SetActive(false);
        }

        public void TakeDamage(float damage) => Stats.health.ReduceCurrent(damage);
        
        public void MoveTo(Hex hex) => HexPosition = hex;

        // View follows model: smoothly track the logical hex position each frame.
        private void Update()
        {
            if (_grid == null) return;
            var targetPos = HexPosition.ToWorld(_grid);
            transform.position = Vector3.Lerp(transform.position, targetPos, _moveLerpSpeed * Time.deltaTime);
        }
    }

    public interface IPawn : IMovable, ICombatParticipant
    {
        IPawnEffect      PawnEffects   { get; }
        IPawnStats       Stats         { get; }
        ITetrisContainer Inventory     { get; }
    }

    public interface ICombatParticipant
    {
        PawnTeam Team { get; }
        event Action OnDefeated;
        void TakeDamage(float damage);
    }
    
    public interface IMovable
    {
        Hex HexPosition { get; }
        void MoveTo(Hex hex);
        TerrainCostConfig   MovementCosts { get; }
    }
}