using System;
using System.Collections.Generic;
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
        [SerializeField] private AnimationCurve _moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private Grid _grid;

        // View-side glide between hex states. The sim stays authoritative on HexPosition (ADR-0002);
        // this is cosmetic interpolation, tick-locked by the caller to the CombatClock interval.
        private readonly MoveInterpolator _move = new();
        private Func<float, float> _ease;

        // The current visual step, exposed read-only so a future telegraph can draw planned movement.
        public Hex  StepFrom   { get; private set; }
        public Hex  StepTo     { get; private set; }
        public bool IsStepping => _move.IsMoving;

        //[field: SerializeField] public PawnConfig        Config        { get; private set; }
        [field: SerializeField, ReadOnly] public PawnTeam          Team          { get; private set; }
        [field: SerializeField, ReadOnly] public Hex               HexPosition   { get; private set; }
        [field: SerializeField, ReadOnly] public TerrainCostConfig    MovementCosts { get; private set; }
        
        // Backed by a concrete serialized field so the live stat block (resources, regen, range)
        // is visible+expandable in the Inspector at runtime — the IPawnStats interface alone is
        // not serializable, so a property-only Stats showed nothing. PawnStats and its Resource/Stat
        // members already carry [SerializeField, ReadOnly, AllowNesting] for this.
        [SerializeField, ReadOnly, AllowNesting] private PawnStats _stats;
        public IPawnStats       Stats         => _stats;
        public ITetrisContainer Inventory     { get; private set; }
        public IPawnEffect      PawnEffects   { get; private set; }

        // Runtime-only mirror of this pawn's resolved attacks (weapon + chain mods → WeaponStats),
        // rebuilt on every inventory change so the live damage/fire-rate is inspectable like _stats.
        // Read-only diagnostic — combat resolves its own stats at fire time; this is not the source.
        [SerializeField, ReadOnly, AllowNesting] private List<ResolvedAttack> _attacks = new();

        [Serializable]
        private struct ResolvedAttack
        {
            public string weapon;
            public float  damage;
            public float  attackSpeed;      // attacks per second (fire interval = 1 / attackSpeed)
            public float  resourceCost;
            public float  resourceGenOnHit;
        }

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
            _stats        = new PawnStats(config);
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

            Inventory.OnContentsChanged += _ => RebuildAttacks();
            RebuildAttacks();

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

        // Refreshes the inspectable attack readout from the pawn's resolved chains. Pure read of the
        // container-owned topology — same WeaponStatResolver combat and the tooltip use.
        private void RebuildAttacks()
        {
            _attacks.Clear();
            if (Inventory == null) return;

            foreach (var chain in Inventory.Topology.Chains)
            {
                var stats = WeaponStatResolver.Resolve(chain);
                _attacks.Add(new ResolvedAttack
                {
                    weapon           = chain.Weapon?.Name ?? "(no weapon)",
                    damage           = stats.Damage,
                    attackSpeed      = stats.AttackSpeed,
                    resourceCost     = stats.ResourceCost,
                    resourceGenOnHit = stats.ResourceGenOnHit,
                });
            }
        }

        public void TakeDamage(float damage) => Stats.health.ReduceCurrent(damage);
        
        // Instantaneous move: commits the logical hex and cancels any glide (spawn, teleport, knockback).
        public void MoveTo(Hex hex)
        {
            HexPosition = hex;
            StepFrom    = hex;
            StepTo      = hex;
            if (_grid != null)
                _move.Begin(hex.ToWorld(_grid), hex.ToWorld(_grid), 0f);
        }

        // Timed move: commits the logical hex and eases the view from the previous hex over `duration`
        // seconds (tick-locked by the caller to the CombatClock interval). Damage/range read the hex,
        // never this glide (ADR-0002).
        public void MoveTo(Hex hex, float duration)
        {
            var from    = HexPosition;
            HexPosition = hex;
            StepFrom    = from;
            StepTo      = hex;
            if (_grid != null)
                _move.Begin(from.ToWorld(_grid), hex.ToWorld(_grid), duration);
        }

        // View follows model: glide toward the logical hex with an eased, tick-locked step; snap when
        // idle or after a teleport. The sim is authoritative on the hex (ADR-0002).
        private void Update()
        {
            if (_grid == null) return;
            _ease ??= _moveEase != null ? _moveEase.Evaluate : t => t;

            transform.position = _move.IsMoving
                ? _move.Advance(Time.deltaTime, _ease)
                : HexPosition.ToWorld(_grid);
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
        void MoveTo(Hex hex, float duration);
        TerrainCostConfig   MovementCosts { get; }
    }
}