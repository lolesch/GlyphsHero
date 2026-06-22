using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.HexGrid;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using Code.Runtime.Pawns;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Owns all combat state for a single pawn.
    /// Created and managed exclusively by CombatCoordinator — never self-owned by Pawn.
    /// Each firing's stats are resolved to a <see cref="WeaponStats"/> value via
    /// <see cref="WeaponStatResolver"/>; the weapon's live MutableFloats are never mutated.
    /// Timed firings ride per-weapon <see cref="CombatClock"/> metronomes advanced by
    /// <see cref="Tick"/> (Candidate #7) — attacks resolve on the same deterministic fixed tick
    /// as movement, not the player-loop Utility Timer. _cadences holds those metronomes;
    /// _cleanupActions tears down reactor event subscriptions; both are cleared on rebuild/stop.
    /// </summary>
    public sealed class PawnCombatController : IPawnCombatController
    {
        private readonly IPawn      _pawn;
        private readonly ITetrisContainer _inventory;
        private readonly IHexGrid         _hexGrid;
        private readonly ICombatEventBus  _eventBus;
        private readonly List<Action>     _cleanupActions = new();
        // Per-weapon attack metronomes; advanced by Tick on the combat clock (Candidate #7).
        private readonly List<CombatClock> _cadences      = new();

        private IPawn _target;
        private bool        _isRunning;
        private PawnRegistry _registry;

        public PawnCombatController(IPawn pawn, IHexGrid hexGrid, ICombatEventBus eventBus, PawnRegistry registry)
        {
            _pawn      = pawn;
            _inventory = pawn.Inventory;
            _hexGrid   = hexGrid;
            _eventBus  = eventBus;
            _registry  = registry;

            _inventory.OnContentsChanged += _ => RebuildChains();
        }

        public void SetCurrentTarget(IPawn target) => _target = target;

        /// <summary>
        /// Advances this pawn's attack metronomes by one combat-clock step. Driven by the
        /// coordinator's master tick (Candidate #7), so attacks fire on the same fixed,
        /// frame-rate-independent tick as movement. A fresh cadence won't reach a whole interval on
        /// the engaging tick, so a just-engaged pawn doesn't fire instantly. No-op when not running
        /// or when the pawn has no timed firings (e.g. reactor-only).
        ///
        /// A fire can cascade into a kill → registry removal → re-evaluation that calls StopCombat on
        /// this very controller, clearing <see cref="_cadences"/> mid-iteration. Advance over a
        /// snapshot and stop the moment this controller is torn down (_isRunning flips false).
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isRunning) return;
            foreach (var cadence in _cadences.ToArray())
            {
                if (!_isRunning) return;
                cadence.Advance(deltaTime);
            }
        }

        public void StartCombat()
        {
            _isRunning = true;
            RebuildChains();
        }

        public void StopCombat()
        {
            _isRunning = false;
            Cleanup();
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void RebuildChains()
        {
            Cleanup();
            if (!_isRunning) return;

            var chains = _inventory.Topology.Chains;
            if (chains.Count == 0)
            {
                Debug.Log("[Combat] No weapons — pawn is not attacking.");
                return;
            }

            // Each chain is one firing: ChainResolver now emits a weapon-rooted firing only when no
            // reactor drives the weapon (timer), else one reactor-rooted firing per reactor event
            // (the timer is suppressed). No duplicate (root, weapon) chains to collapse, and stats
            // are resolved to a value per firing — no weapon mutation, nothing to revert in Cleanup.
            foreach (var firing in chains)
            {
                var stats = WeaponStatResolver.Resolve(firing);

                switch (firing.Root)
                {
                    case IWeaponItem:
                        BuildTimedChain(firing, stats);
                        break;
                    case IReactorItem reactor:
                        BuildReactor(reactor, firing, stats);
                        break;
                }
            }
        }

        private void BuildTimedChain(IItemChain chain, WeaponStats stats)
        {
            var weapon = chain.Weapon;
            var (costResource, genResource) = ResolveChainResources(chain);

            // Attack cadence rides the combat clock (Candidate #7): Tick advances this metronome by
            // the master tick interval, firing 0..N times per tick (carrying the remainder), so
            // attacks resolve on the same deterministic fixed tick as movement. stats is an immutable
            // value resolved once per rebuild, so the interval is fixed for the cadence's lifetime.
            var cadence = new CombatClock(1f / stats.AttackSpeed);
            cadence.OnTick += () =>
            {
                if (CanFire(stats.ResourceCost, costResource))
                    Fire(chain, weapon, stats, costResource, genResource);
            };

            _cadences.Add(cadence);
        }

        private void BuildReactor(IReactorItem reactor, IItemChain chain, WeaponStats stats)
        {
            var weapon = chain.Weapon;
            var (costResource, genResource) = ResolveChainResources(chain);

            switch (reactor.ReactorType)
            {
                case ReactorType.OnSelfHit:
                {
                    void OnHealthChanged(float prev, float curr, float _)
                    {
                        if (curr < prev && CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    _pawn.Stats.health.OnCurrentChanged += OnHealthChanged;
                    _cleanupActions.Add(() => _pawn.Stats.health.OnCurrentChanged -= OnHealthChanged);
                    break;
                }
                case ReactorType.OnManaDeplete:
                {
                    void OnManaDeplete()
                    {
                        if (CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    _pawn.Stats.mana.OnDepleted += OnManaDeplete;
                    _cleanupActions.Add(() => _pawn.Stats.mana.OnDepleted -= OnManaDeplete);
                    break;
                }
                case ReactorType.OnEnemyDeath:
                {
                    void OnDefeated(IPawn unit)
                    {
                        if (unit.Team == _pawn.Team) return;
                        if (CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    _eventBus.OnUnitDefeated += OnDefeated;
                    _cleanupActions.Add(() => _eventBus.OnUnitDefeated -= OnDefeated);
                    break;
                }
                case ReactorType.OnAllyAttacks:
                {
                    void OnAllyAttacked(IPawn unit)
                    {
                        if (unit.Team != _pawn.Team || unit == _pawn) return;
                        if (CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    _eventBus.OnUnitAttacked += OnAllyAttacked;
                    _cleanupActions.Add(() => _eventBus.OnUnitAttacked -= OnAllyAttacked);
                    break;
                }
                case ReactorType.OnAllyKills:
                {
                    void OnAllyKill(IPawn unit)
                    {
                        if (unit.Team != _pawn.Team || unit == _pawn) return;
                        if (CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    // OnAllyKills = ally attacks that result in a defeat.
                    // The kill event isn't separately tracked; subscribe to OnUnitDefeated
                    // and check if the last attacker was an ally — not yet tracked on the bus.
                    // TODO: add ICombatEventBus.OnUnitKilled(killer, victim) when kill attribution is needed.
                    Debug.LogWarning("[Combat] OnAllyKills reactor wired to OnUnitDefeated without kill attribution — extend ICombatEventBus when ready.");
                    _eventBus.OnUnitDefeated += OnAllyKill;
                    _cleanupActions.Add(() => _eventBus.OnUnitDefeated -= OnAllyKill);
                    break;
                }
                case ReactorType.OnNearbyEnemyDies:
                {
                    void OnNearbyEnemyDefeated(IPawn unit)
                    {
                        if (unit.Team == _pawn.Team) return;
                        if (_hexGrid == null) return;
                        if (_pawn.HexPosition.Distance(unit.HexPosition) > /*reactor.Range*/ 1) return;
                        if (CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource, genResource);
                    }
                    _eventBus.OnUnitDefeated += OnNearbyEnemyDefeated;
                    _cleanupActions.Add(() => _eventBus.OnUnitDefeated -= OnNearbyEnemyDefeated);
                    break;
                }
            }
        }

        private void Fire(IItemChain chain, IWeaponItem weapon, WeaponStats stats, Resource costResource, Resource genResource)
        {
            if (_target == null) return;
            costResource.ReduceCurrent(stats.ResourceCost);
            _target.TakeDamage(stats.Damage);
            genResource.IncreaseCurrent(stats.ResourceGenOnHit);

            _eventBus.PublishAttacked(_pawn);
            _eventBus.PublishHit(_pawn, _target);

            FirePayloads(chain, weapon, stats, costResource, genResource);
        }

        private void FirePayloads(IItemChain chain, IWeaponItem rootWeapon, WeaponStats rootStats, Resource costResource, Resource genResource)
        {
            foreach (var item in chain.Modifiers)
            {
                if (item is not IWeaponItem payload || item == rootWeapon) continue;
                if (!CanFire(payload.ResourceCost, costResource)) continue;
                if (!EvaluatePayloadCondition(payload, rootStats.Damage)) continue;

                var behavior = payload.Payload;
                costResource.ReduceCurrent(payload.ResourceCost);

                var (targets, hexes) = ResolvePayloadTargets(behavior);
                foreach (var target in targets)
                {
                    target.TakeDamage(payload.Damage);
                    genResource.IncreaseCurrent(payload.ResourceGenOnHit);
                    _eventBus.PublishHit(_pawn, target);
                }

                if (behavior != null)
                    foreach (var effect in behavior.Effects)
                        ExecuteEffect(effect, targets, hexes);
            }
        }

        private (List<IPawn> targets, List<Hex> hexes) ResolvePayloadTargets(PayloadBehavior behavior)
        {
            if (behavior == null || _target == null)
                return FallbackToSingleTarget();

            var otherTeam = _target.Team == PawnTeam.Player ? PawnTeam.Enemy  : PawnTeam.Player;
            switch (behavior.Targeting)
            {
                case PayloadTargeting.Single:
                    return FallbackToSingleTarget();

                case PayloadTargeting.Self:
                    return (new List<IPawn> { _pawn }, new List<Hex> { _pawn.HexPosition });

                case PayloadTargeting.Aoe:
                {
                    var aoeTargets = TargetSelector
                        .GetPawnsInRange(_target.HexPosition, behavior.Range, _registry.allPawns, otherTeam).ToList();
                    return aoeTargets.Count > 0 ? (aoeTargets, _target.HexPosition.HexRange(behavior.Range)) : FallbackToSingleTarget();
                }

                case PayloadTargeting.Line:
                {
                    if (_hexGrid == null) { LogMissingHexGrid(); return FallbackToSingleTarget(); }
                    var lineHexes  = _pawn.HexPosition.HexLine(_target.HexPosition);
                    var lineTargets = new List<IPawn>();
                    foreach (var hex in lineHexes)
                        foreach (var pawn in TargetSelector.GetPawnsInRange(hex, 0, _registry.allPawns, otherTeam))
                            if ( !lineTargets.Contains(pawn))
                                lineTargets.Add(pawn);
                    return (lineTargets, lineHexes);
                }

                default:
                    return FallbackToSingleTarget();
            }
        }

        private (List<IPawn>, List<Hex>) FallbackToSingleTarget() =>
            _target == null
                ? (new List<IPawn>(), new List<Hex>())
                : (new List<IPawn> { _target }, new List<Hex> { _target.HexPosition });

        private void ExecuteEffect(PayloadEffect effect, List<IPawn> targets, List<Hex> hexes)
        {
            switch (effect)
            {
                case StatusPayloadEffect:
                    Debug.LogWarning("[Combat] StatusPayloadEffect not yet wired — status system pending.");
                    break;
                case PositionPayloadEffect position:
                    ApplyPositionEffect(position, targets);
                    break;
                case TerrainPayloadEffect terrain:
                    ApplyTerrainEffect(terrain, hexes);
                    break;
            }
        }

        private void ApplyPositionEffect(PositionPayloadEffect effect, List<IPawn> targets)
        {
            foreach (var target in targets)
            {
                switch (effect.EffectType)
                {
                    case PositionEffectType.Push:
                    case PositionEffectType.Pull:
                        ApplyDisplacement(target, effect);
                        break;
                    case PositionEffectType.Stun:
                        Debug.LogWarning("[Combat] Stun not yet implemented.");
                        break;
                }
            }
        }

        private void ApplyDisplacement(IPawn target, PositionPayloadEffect effect)
        {
            var line = _pawn.HexPosition.HexLine(target.HexPosition);
            if (line.Count < 2) return;

            var step = line[1].Subtract(line[0]);
            if (effect.EffectType == PositionEffectType.Pull)
                step = new Hex(-step.q, -step.r);

            target.MoveTo(target.HexPosition.Add(step.Scale(effect.Distance)));
        }

        private void ApplyTerrainEffect(TerrainPayloadEffect effect, List<Hex> hexes)
        {
            if (_hexGrid == null) { LogMissingHexGrid(); return; }
            foreach (var hex in hexes)
                _hexGrid.SetTerrain(hex, effect.TerrainType);
        }

        private bool EvaluatePayloadCondition(IWeaponItem payload, float rootDamage)
        {
            var behavior = payload.Payload;
            if (behavior == null) return true;
            return behavior.Condition switch
            {
                ConditionType.None            => true,
                ConditionType.ResourceFull    => _pawn.Stats.mana.IsFull,
                ConditionType.ResourceBelow   => _pawn.Stats.mana.Percentage < behavior.ConditionThreshold,
                ConditionType.ResourceAbove   => _pawn.Stats.mana.Percentage > behavior.ConditionThreshold,
                ConditionType.DamageAbove     => rootDamage >= behavior.ConditionThreshold,
                ConditionType.HasStatusEffect => false,
                _                             => false,
            };
        }

        private static bool CanFire(float resourceCost, Resource costResource) =>
            costResource.CanSpend(resourceCost);

        private (Resource costResource, Resource genResource) ResolveChainResources(IItemChain chain)
        {
            // Converter will override resource targets here when implemented.
            return (_pawn.Stats.mana, _pawn.Stats.mana);
        }

        private static void LogMissingHexGrid() =>
            Debug.LogWarning("[Combat] IHexGrid not set — payload targeting falls back to single-target.");

        private void Cleanup()
        {
            foreach (var action in _cleanupActions) action();
            _cleanupActions.Clear();
            // CombatClocks hold no global registration — dropping the references retires them.
            _cadences.Clear();
        }
    }

    public interface IPawnCombatController
    {
        void SetCurrentTarget(IPawn target);
        void StartCombat();
        void StopCombat();
        void Tick(float deltaTime);
    }
}