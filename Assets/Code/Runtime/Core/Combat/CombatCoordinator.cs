using System;
using System.Collections.Generic;
using Code.Runtime.Modules.HexGrid;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Pawns;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Scene-level authority for all combat.
    /// Owns PawnCombatController lifetimes, per-unit targeting, and resolution-tick movement.
    /// CombatPhase calls StartCombat / StopCombat. Pawns self-register via Register / Unregister.
    ///
    /// Movement runs on a project-side <see cref="CombatClock"/> (ADR-0001, Candidate #5): every
    /// tick each seeking pawn accrues move-readiness and proposes a single step against a frozen
    /// snapshot, then <see cref="MovementResolver"/> resolves all proposals together (read-then-write,
    /// closest-to-target wins contested hexes). This replaced the per-pawn movement <c>Timer</c>s and
    /// the reservation bookkeeping (reserved/claimed hex sets + on-arrival re-check) that existed only
    /// to compensate for stale per-pawn decisions. Attacks ride the same clock (Candidate #7): each
    /// tick advances every controller's per-weapon attack metronome, so firing and movement resolve
    /// on one deterministic, frame-rate-independent tick instead of a separate Utility <c>Timer</c>.
    /// </summary>
    public sealed class CombatCoordinator : MonoBehaviour, ICombatCoordinator
    {
        private PawnRegistry _registry;
        [SerializeField] private HexGridController _hexGrid;
        // Resolution tick length, decoupled from frame rate. 0.1s = 10 ticks/sec; a pawn with
        // movementSpeed 1 over cost-1 terrain banks one hex per second. View lerps between ticks.
        [SerializeField] private float _tickInterval = 0.1f;

        private IReadOnlyList<IPawn> playerUnits => _registry.playerPawns;
        private IReadOnlyList<IPawn> enemyUnits  => _registry.enemyPawns;

        // Per-unit combat controllers
        private readonly Dictionary<IPawn, PawnCombatController> _controllers  = new();
        // Where each unit currently stands — the movement snapshot's occupancy (no longer a reservation).
        private readonly Dictionary<IPawn, Hex>                  _claimedHexes = new();
        // Banked move-readiness for seeking units; reset to 0 once a unit engages.
        private readonly Dictionary<IPawn, float>               _readiness    = new();
        // Stable per-combat registration index — the deterministic contested-hex tiebreak.
        private readonly Dictionary<IPawn, int>                  _pawnIds      = new();
        // Units currently firing (engaged) — guards against rebuilding chains every tick.
        private readonly HashSet<IPawn>                          _engaged      = new();
        // Minimum active-weapon reach per unit — the ring a pawn closes to so all its weapons fire.
        private readonly Dictionary<IPawn, int>                  _minReach     = new();
        // Reused per-tick snapshot of controllers so a kill-cascade can't mutate the live enumerator.
        private readonly List<PawnCombatController>              _tickBuffer   = new();

        private ICombatEventBus _eventBus;
        private CombatClock     _clock;
        private bool            _isRunning;
        private int             _nextPawnId;

        /// <summary>Raised once when a team is wiped — victory (enemies gone) or defeat (player gone).</summary>
        public event Action<CombatOutcome> OnCombatEnded;

        // Temporary movement diagnostics — flip off once movement is verified.
        private const bool LogMovement = false;

        private void Awake()
        {
            _eventBus       = new CombatEventBus();
            _clock          = new CombatClock(_tickInterval);
            _clock.OnTick  += Tick;
        }

        public void Initialize(PawnRegistry registry)
        {
            _registry = registry;
            _registry.OnPawnRegistered += InsertUnit;
            // Automatically clean up dictionaries when a pawn is removed from the registry
            _registry.OnPawnUnregistered += HandlePawnRemoved;
        }

        // ── Lifecycle ────────────────────────────────────────────────────

        public void StartCombat()
        {
            _isRunning = true;
            _clock.Reset();

            foreach (var unit in playerUnits) InitUnit(unit);
            foreach (var unit in enemyUnits)  InitUnit(unit);

            // Enemies always start a combat at full health (player current carries from spawn —
            // injuries / low-current threshold builds are intentional and left untouched).
            foreach (var unit in enemyUnits) unit.Stats.health.RefillCurrent();

            foreach (var unit in playerUnits) EvaluateEngagement(unit, enemyUnits);
            foreach (var unit in enemyUnits)  EvaluateEngagement(unit, playerUnits);

            // Handle an encounter that begins with an empty side.
            TryResolveOutcome();
        }

        public void StopCombat()
        {
            _isRunning = false;

            foreach (var (_, controller) in _controllers) controller.StopCombat();

            _controllers.Clear();
            _claimedHexes.Clear();
            _readiness.Clear();
            _pawnIds.Clear();
            _engaged.Clear();
            _minReach.Clear();
            _nextPawnId = 0;
            _clock.Reset();
        }

        // Drives the resolution clock; the clock fires Tick once per fixed interval.
        private void Update()
        {
            if (!_isRunning) return;
            _clock.Advance(Time.deltaTime);
        }

        // ── Internal ─────────────────────────────────────────────────────

        private void InsertUnit(IPawn unit)
        {
            InitUnit(unit);

            foreach (var u in playerUnits) EvaluateEngagement(u, enemyUnits);
            foreach (var u in enemyUnits)  EvaluateEngagement(u, playerUnits);
        }

        private void InitUnit(IPawn unit)
        {
            _claimedHexes[unit] = unit.HexPosition;
            _readiness[unit]    = 0f;
            _minReach[unit]     = ResolveMinReach(unit);
            _pawnIds[unit]      = _nextPawnId++;
            _controllers[unit]  = new PawnCombatController(unit, _hexGrid, _eventBus, _registry);
        }

        /// <summary>
        /// Decides whether a unit is firing or seeking. A target within reach engages the unit
        /// (chains start firing on the combat clock); otherwise the unit disengages and is moved
        /// by the resolution tick. Returns true when engaged. Idempotent — chains are (re)built only
        /// on the not-engaged → engaged transition, so calling it every tick doesn't reset attacks.
        /// </summary>
        private bool EvaluateEngagement(IPawn unit, IReadOnlyList<IPawn> opponents)
        {
            if (!_isRunning) return false;

            var target     = TargetSelector.Select(unit, opponents, _minReach[unit]);
            var controller = _controllers[unit];

            if (target != null)
            {
                controller.SetCurrentTarget(target);
                if (_engaged.Add(unit))
                    controller.StartCombat();
                _readiness[unit] = 0f; // not moving while engaged
                return true;
            }

            if (_engaged.Remove(unit))
                controller.StopCombat();
            return false;
        }

        /// <summary>
        /// One resolution tick: gather every seeking unit's proposed step against the frozen
        /// snapshot, resolve them together, then apply. Read-then-write — no unit moves until all
        /// proposals are in, so movement-vs-movement is fully synchronised within the tick.
        /// </summary>
        private void Tick()
        {
            if (!_isRunning) return;

            ResolveMovement();
            AdvanceAttacks();
            RegenerateResources();
        }

        /// <summary>
        /// Health regen on the combat clock: every alive pawn restores healthRegen-per-second,
        /// scaled by the tick interval. Combat-only — the clock advances solely while combat runs.
        /// Regen only raises current, so it can't unregister a pawn mid-iteration.
        /// </summary>
        private void RegenerateResources()
        {
            foreach (var unit in playerUnits)
                unit.Stats.health.Regenerate(unit.Stats.healthRegen, _clock.TickInterval);
            foreach (var unit in enemyUnits)
                unit.Stats.health.Regenerate(unit.Stats.healthRegen, _clock.TickInterval);
        }

        private void ResolveMovement()
        {
            var movers = new List<Mover>();
            var pawns  = new List<IPawn>();

            // GatherMovers also (re)evaluates engagement for every unit, so engaged/seeking state is
            // current before either system resolves this tick.
            GatherMovers(playerUnits, enemyUnits, movers, pawns);
            GatherMovers(enemyUnits,  playerUnits, movers, pawns);

            if (movers.Count == 0) return;

            var results = MovementResolver.Resolve(movers);
            for (var i = 0; i < results.Count; i++)
            {
                var unit = pawns[i];
                var res  = results[i];
                _readiness[unit] = res.Readiness;

                if (!res.Stepped) continue;

                _claimedHexes[unit] = res.Position;
                unit.MoveTo(res.Position);

                if (LogMovement)
                    Debug.Log($"[Move] {_pawnIds[unit]} stepped to {res.Position}");
            }
        }

        /// <summary>
        /// Advances every controller's attack metronomes by one tick (Candidate #7). Engaged pawns
        /// fire their ready attacks; seeking pawns hold no cadences, so this is a no-op for them.
        /// Runs after movement so a pawn that just engaged this tick is already firing-ready.
        /// </summary>
        private void AdvanceAttacks()
        {
            // A fire can kill a pawn → HandlePawnRemoved mutates _controllers (and may end combat)
            // mid-iteration; advance over a snapshot. Removed/disengaged controllers are already
            // torn down, so their Tick is a guarded no-op.
            _tickBuffer.Clear();
            _tickBuffer.AddRange(_controllers.Values);
            foreach (var controller in _tickBuffer)
            {
                if (!_isRunning) return;
                controller.Tick(_clock.TickInterval);
            }
        }

        private void GatherMovers(IReadOnlyList<IPawn> units, IReadOnlyList<IPawn> opponents,
            List<Mover> movers, List<IPawn> pawns)
        {
            foreach (var unit in units)
            {
                // Engaged units fire instead of moving; this also keeps firing state current.
                if (EvaluateEngagement(unit, opponents)) continue;

                var nearest = FindNearest(unit, opponents);
                if (nearest == null) continue;

                var nextHex     = ResolveNextHex(unit, nearest);
                var nextHexValid = nextHex.IsValid && nextHex != unit.HexPosition;
                var stepCost    = nextHexValid ? StepCost(unit, nextHex) : 1;

                movers.Add(new Mover
                {
                    Id            = _pawnIds[unit],
                    Position      = unit.HexPosition,
                    Target        = nearest.HexPosition,
                    Reach         = _minReach[unit],
                    NextStep      = nextHexValid ? nextHex : Hex.Invalid,
                    NextStepCost  = stepCost,
                    Readiness     = _readiness[unit],
                    ReadinessGain = Mathf.Max(unit.Stats.movementSpeed, 0f) * _clock.TickInterval,
                });
                pawns.Add(unit);
            }
        }

        private int StepCost(IPawn unit, Hex hex)
        {
            var terrainType = _hexGrid.GetTerrain(hex);
            return unit.MovementCosts?.GetCost(terrainType) ?? 1;
        }

        /// <summary>
        /// Single-unit A* against the snapshot; returns the first step toward <paramref name="destination"/>,
        /// or <see cref="Hex.Invalid"/> when blocked / no path. No reservations — other units appear
        /// only at their current positions and are hard obstacles: A* routes around an empty detour,
        /// and a fully walled-off pawn gets no path and idles (Decision 5). One step per tick against a
        /// fresh snapshot, so a path that an ally is blocking simply opens once the ally moves.
        /// </summary>
        private Hex ResolveNextHex(IPawn unit, IPawn destination)
        {
            var occupiedSet = BuildOccupancySet(unit);

            var path = HexPathfinder.FindPath(
                unit.HexPosition,
                destination.HexPosition,
                occupiedSet,         // occupied hexes are impassable: not traversable, not a landing hex
                _hexGrid,
                null,
                unit.MovementCosts);

            if (path == null) return Hex.Invalid;

            // Walk the parent chain back to find the first step after the start.
            var node = path;
            while (node.Parent != null && node.Parent.Hex != unit.HexPosition)
                node = node.Parent;

            return node.Hex == unit.HexPosition ? Hex.Invalid : node.Hex;
        }

        /// <summary>Current positions of all units except the mover — the frozen movement snapshot.</summary>
        private HashSet<Hex> BuildOccupancySet(IPawn movingUnit)
        {
            var occupied = new HashSet<Hex>();
            foreach (var (unit, hex) in _claimedHexes)
                if (unit != movingUnit) occupied.Add(hex);
            return occupied;
        }

        private IPawn FindNearest(IPawn unit, IReadOnlyList<IPawn> candidates)
        {
            IPawn nearest  = null;
            var   bestDist = int.MaxValue;

            foreach (var candidate in candidates)
            {
                var dist = unit.HexPosition.Distance(candidate.HexPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                nearest  = candidate;
            }

            return nearest;
        }

        private void HandlePawnRemoved(IPawn unit)
        {
            if (_controllers.TryGetValue(unit, out var controller))
            {
                controller.StopCombat();
                _controllers.Remove(unit);
            }

            _claimedHexes.Remove(unit);
            _readiness.Remove(unit);
            _pawnIds.Remove(unit);
            _engaged.Remove(unit);
            _minReach.Remove(unit);

            _eventBus.PublishDefeated(unit);

            // A team may have just been wiped — end combat instead of re-evaluating survivors.
            if (TryResolveOutcome()) return;

            // Re-evaluate all surviving units — their target may be gone or a new gap opened.
            foreach (var u in playerUnits) EvaluateEngagement(u, enemyUnits);
            foreach (var u in enemyUnits)  EvaluateEngagement(u, playerUnits);
        }

        /// <summary>
        /// Ends combat if a team is wiped. Returns true when an outcome was raised, so callers
        /// stop touching combat state (StopCombat clears it during the OnCombatEnded handler).
        /// </summary>
        private bool TryResolveOutcome()
        {
            if (!_isRunning) return false;

            var outcome = CombatOutcomeResolver.Resolve(playerUnits.Count, enemyUnits.Count);
            if (outcome == null) return false;

            EndCombat(outcome.Value);
            return true;
        }

        private void EndCombat(CombatOutcome outcome)
        {
            _isRunning = false; // stop further evaluation + guard against re-entrancy
            OnCombatEnded?.Invoke(outcome);
        }

        /// <summary>
        /// Minimum effective reach across the unit's active weapons (ADR-0001, Decision 3): a pawn
        /// closes until <em>all</em> its weapons can fire. Reach is a pawn stat (Decision 2) — a
        /// range-fixed weapon (melee/adjacent, intrinsic Payload.Range ≤ 1) reaches 1; a range-scaling
        /// weapon reaches the pawn's range stat. v1 classifies range-fixed by the weapon's existing
        /// Payload.Range; the full delivery-pattern split (Projectile/Beam/Arc) is the Decision 2b follow-up.
        /// </summary>
        private static int ResolveMinReach(IPawn unit)
        {
            var chains    = unit.Inventory.Topology.Chains;
            var pawnRange = Mathf.Max(1, Mathf.RoundToInt(unit.Stats.range));

            var minReach = int.MaxValue;
            foreach (var chain in chains)
            {
                var rangeFixed = chain.Weapon.Payload.Range <= 1;
                var reach      = rangeFixed ? 1 : pawnRange;
                if (reach < minReach) minReach = reach;
            }

            return minReach == int.MaxValue ? 1 : minReach;
        }
    }

    public interface ICombatCoordinator
    {
        event Action<CombatOutcome> OnCombatEnded;

        void StartCombat();
        void StopCombat();
    }
}
