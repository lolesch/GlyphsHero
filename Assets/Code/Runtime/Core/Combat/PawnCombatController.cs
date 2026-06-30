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
            var weapon       = chain.Weapon;
            var costResource = ResolveChainResources(stats);

            // Attack cadence rides the combat clock (Candidate #7): Tick advances this metronome by
            // the master tick interval, firing 0..N times per tick (carrying the remainder), so
            // attacks resolve on the same deterministic fixed tick as movement. stats is an immutable
            // value resolved once per rebuild, so the interval is fixed for the cadence's lifetime.
            var cadence = new CombatClock(1f / stats.AttackSpeed);
            cadence.OnTick += () =>
            {
                if (CanFire(stats.ResourceCost, costResource))
                    Fire(chain, weapon, stats, costResource);
            };

            _cadences.Add(cadence);
        }

        private void BuildReactor(IReactorItem reactor, IItemChain chain, WeaponStats stats)
        {
            var weapon       = chain.Weapon;
            var costResource = ResolveChainResources(stats);

            switch (reactor.ReactorType)
            {
                case ReactorType.OnSelfHit:
                {
                    void OnHealthChanged(float prev, float curr, float _)
                    {
                        if (curr < prev && CanFire(stats.ResourceCost, costResource))
                            Fire(chain, weapon, stats, costResource);
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
                            Fire(chain, weapon, stats, costResource);
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
                            Fire(chain, weapon, stats, costResource);
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
                            Fire(chain, weapon, stats, costResource);
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
                            Fire(chain, weapon, stats, costResource);
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
                            Fire(chain, weapon, stats, costResource);
                    }
                    _eventBus.OnUnitDefeated += OnNearbyEnemyDefeated;
                    _cleanupActions.Add(() => _eventBus.OnUnitDefeated -= OnNearbyEnemyDefeated);
                    break;
                }
            }
        }

        private void Fire(IItemChain chain, IWeaponItem weapon, WeaponStats stats, Resource costResource)
        {
            if (_target == null) return;

            // ADR-0006: one fail-forward walk decides the whole attack's spend. The weapon's resolved cost
            // is the root gate; each payload adds a marginal cost over the shared pool; an unaffordable node
            // prunes its subtree. Decide first, spend once, then detonate only what the walk funded.
            // reactorMods is null because the Reactor's cost modifier (Decision 6 — a ManaCost input mod,
            // typically PercentMult to tax frequent triggers) is already folded into stats.ResourceCost by
            // WeaponStatResolver, so it is the effective base here; passing it again would double-count.
            var (roots, ordered) = BuildPayloadCostTree(chain, weapon);
            var poolBalance = SpendableBalance(costResource, stats.CostResource);
            var result = PropagationCostResolver.Resolve(stats.ResourceCost, null, roots, poolBalance);

            if (!result.RootFired) return;
            costResource.ReduceCurrent(result.TotalSpent);

            // Hex-occupancy damage (ADR-0002/0004): the weapon's delivery mask, centred on its Anchor
            // (target by default, the firing pawn for anchor-Origin) and resolved against its Affinity's
            // side, gives a set of covered hexes; every pawn of that side standing on them is hit.
            // Anchor and Affinity are independent (ADR-0004 §3): a bare hostile Single anchored on the
            // target hits the locked target as before; anchor-Origin + Self is the deliberate self-hurt
            // build-around (centres on, and hits, the caster). Materialise before dealing damage: a kill
            // can cascade into a registry change, and we must not enumerate allPawns while it mutates.
            var anchor  = DeliveryAnchor.Resolve(_pawn.HexPosition, _target.HexPosition, stats.Anchor);
            var covered = DeliveryResolver.CoveredHexes(_pawn.HexPosition, anchor, stats.Delivery);
            var targets = ResolveTargets(covered, stats.Affinity);

            _eventBus.PublishAttacked(_pawn);
            foreach (var target in targets)
            {
                target.TakeDamage(stats.Damage);
                _eventBus.PublishHit(_pawn, target);
            }

            // Apply the root weapon's on-hit payload effects (e.g. ResourcePayloadEffect for leech/gen).
            // Gain-on-hit is now authored as an effect on the weapon's Payload.Effects list (ADR-0005 §3).
            var rootEffects = weapon.Payload?.Effects;
            if (rootEffects != null)
                foreach (var effect in rootEffects)
                    ExecuteEffect(effect, targets, covered, stats.Damage);

            FirePayloads(result.FiredNodes, ordered);
        }

        private PawnTeam EnemyTeam => _pawn.Team == PawnTeam.Player ? PawnTeam.Enemy : PawnTeam.Player;

        /// <summary>
        /// The pawns a delivery hits: those of its <see cref="Affinity"/> side standing on the covered
        /// footprint (ADR-0004 §3). Hostile resolves against the enemy team; Friendly/Self against the
        /// caster's own team — so an Origin-anchored Self delivery hits the firing pawn (the deliberate
        /// self-hurt build-around). Materialised here so a kill-cascade can't mutate allPawns mid-enumerate.
        /// </summary>
        // TODO: composite affinities (e.g. Hostile|Self for recoil) require iterating both teams and
        // deduplicating. For now only single-bit affinities are supported; the caster-side check wins.
        private List<IPawn> ResolveTargets(IReadOnlyList<Hex> covered, Affinity affinity)
        {
            var team = DeliveryAffinity.TargetsCasterSide(affinity) ? _pawn.Team : EnemyTeam;
            return TargetSelector.PawnsOnHexes(covered, _registry.allPawns, team).ToList();
        }

        private void FirePayloads(IReadOnlyCollection<CostNode> firedNodes,
            IReadOnlyList<(CostNode node, IWeaponItem payload)> ordered)
        {
            // The walker already chose which payloads the pool funded and spent for them; here we only
            // detonate those, in propagation order. A payload is a child delivery (ADR-0002/0004): its own
            // pattern mask + ShapeSize + Affinity + Anchor, centred on that Anchor (target by default, the
            // firing pawn for anchor-Origin — a Return) and resolved by hex-occupancy like the root weapon.
            // Anchor is independent of Affinity (ADR-0004 §3). Aoe (a disk) is available here, not on weapons.
            var fired = firedNodes as ISet<CostNode> ?? new HashSet<CostNode>(firedNodes);
            foreach (var (node, payload) in ordered)
            {
                if (!fired.Contains(node)) continue;

                var behavior   = payload.Payload;
                var pattern    = behavior?.Delivery ?? DeliveryPattern.Single;
                var affinity   = behavior?.Affinity ?? Affinity.Hostile;
                var anchorAxis = behavior?.Anchor   ?? Anchor.Target;
                var shapeSize  = behavior?.ShapeSize ?? 0;
                var anchor     = DeliveryAnchor.Resolve(_pawn.HexPosition, _target.HexPosition, anchorAxis);
                var covered    = DeliveryResolver.CoveredHexes(_pawn.HexPosition, anchor, pattern, shapeSize);
                var targets    = ResolveTargets(covered, affinity);
                var damage     = (float)payload.Damage;

                foreach (var target in targets)
                {
                    target.TakeDamage(damage);
                    _eventBus.PublishHit(_pawn, target);
                }

                if (behavior != null)
                    foreach (var effect in behavior.Effects)
                        ExecuteEffect(effect, targets, covered, damage);
            }
        }

        /// <summary>
        /// Extracts the chain's payloads (every weapon modifier that isn't the root) into the cost tree the
        /// <see cref="PropagationCostResolver"/> walks — each payload's authored cost
        /// (<see cref="Code.Data.Items.Weapon.PayloadBehavior.CostValue"/>/<c>CostType</c>) becomes a
        /// <see cref="Modifier"/> here, since the Data assembly is dependency-free. Propagation order is
        /// chain order; with no Splitter authored yet this is one linear lineage (ADR-0006).
        /// </summary>
        private static (IReadOnlyList<CostNode> roots, IReadOnlyList<(CostNode node, IWeaponItem payload)> ordered)
            BuildPayloadCostTree(IItemChain chain, IWeaponItem rootWeapon)
        {
            var payloads = new List<(Modifier cost, IWeaponItem payload)>();
            foreach (var item in chain.Modifiers)
            {
                if (item is not IWeaponItem w || w == rootWeapon) continue;
                var behavior = w.Payload;
                var cost = new Modifier(behavior?.CostValue ?? 0f, behavior?.CostType ?? ModifierType.FlatAdd, Guid.NewGuid());
                payloads.Add((cost, w));
            }
            return PayloadCostTree.BuildLineage(payloads);
        }

        /// <summary>
        /// The pool balance the walker may spend. Mirrors <see cref="Resource.CanSpend"/>: a Health cost
        /// pool (blood magic) must never be spent to 0, so the walker sees a hair under full health; Mana
        /// spends down to empty.
        /// </summary>
        private static float SpendableBalance(Resource costResource, ResourceType pool) =>
            pool == ResourceType.Health
                ? Mathf.Max(0f, costResource.CurrentValue - 0.0001f)
                : costResource.CurrentValue;

        private void ExecuteEffect(PayloadEffect effect, List<IPawn> targets, IReadOnlyList<Hex> hexes, float damageDealt = 0f)
        {
            switch (effect)
            {
                case ResourcePayloadEffect resourceEffect:
                {
                    // Gain-on-hit: the caster recovers resources once per target hit (ADR-0005 §3).
                    var pool = resourceEffect.Pool switch
                    {
                        ResourceType.Health => _pawn.Stats.health,
                        _                   => _pawn.Stats.mana,
                    };
                    foreach (var _ in targets)
                        pool.IncreaseCurrent(resourceEffect.ComputeGain(damageDealt));
                    break;
                }
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

        private void ApplyTerrainEffect(TerrainPayloadEffect effect, IReadOnlyList<Hex> hexes)
        {
            if (_hexGrid == null) { LogMissingHexGrid(); return; }
            foreach (var hex in hexes)
                _hexGrid.SetTerrain(hex, effect.TerrainType);
        }

        private static bool CanFire(float resourceCost, Resource costResource) =>
            costResource.CanSpend(resourceCost);

        private Resource ResolveChainResources(WeaponStats stats) =>
            stats.CostResource switch
            {
                ResourceType.Health => _pawn.Stats.health,
                _                   => _pawn.Stats.mana,
            };

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