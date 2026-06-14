using System;
using System.Collections.Generic;
using Code.Runtime.Modules.HexGrid;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Pawns;
using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Timer;
using UnityEngine;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Scene-level authority for all combat.
    /// Owns PawnCombatController lifetimes, per-unit targeting, hex reservation,
    /// and movement timers. CombatPhase calls StartCombat / StopCombat.
    /// Pawns self-register via Register / Unregister.
    /// </summary>
    public sealed class CombatCoordinator : MonoBehaviour, ICombatCoordinator
    {
        private PawnRegistry _registry;
        [SerializeField] private HexGridController _hexGrid;

        private IReadOnlyList<IPawn> playerUnits => _registry.playerPawns;
        private IReadOnlyList<IPawn> enemyUnits  => _registry.enemyPawns;

        // Per-unit combat controllers
        private readonly Dictionary<IPawn, PawnCombatController> _controllers    = new();
        // Where each unit currently stands (updated on MoveTo)
        private readonly Dictionary<IPawn, Hex>                  _claimedHexes   = new();
        // Destination reserved by a unit that is currently mid-movement
        private readonly Dictionary<IPawn, Hex>                  _reservedHexes  = new();
        private readonly Dictionary<IPawn, ITimer>               _movementTimers = new();
        // Max weapon range per unit — derived from chains, refreshed on RebuildChains
        private readonly Dictionary<IPawn, int>                  _maxWeaponRange = new();

        private ICombatEventBus _eventBus;
        private bool            _isRunning;

        /// <summary>Raised once when a team is wiped — victory (enemies gone) or defeat (player gone).</summary>
        public event Action<CombatOutcome> OnCombatEnded;

        // Temporary movement diagnostics — flip off once movement is verified.
        private const bool LogMovement = false;

        private void Awake()
        {
            _eventBus       = new CombatEventBus();
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

            foreach (var unit in playerUnits) InitUnit(unit);
            foreach (var unit in enemyUnits)  InitUnit(unit);

            foreach (var unit in playerUnits) EvaluateUnit(unit, enemyUnits);
            foreach (var unit in enemyUnits)  EvaluateUnit(unit, playerUnits);

            // Handle an encounter that begins with an empty side.
            TryResolveOutcome();
        }

        public void StopCombat()
        {
            _isRunning = false;

            foreach (var (_, controller) in _controllers)  controller.StopCombat();
            foreach (var (_, timer)      in _movementTimers) timer.Stop();

            _controllers.Clear();
            _movementTimers.Clear();
            _claimedHexes.Clear();
            _reservedHexes.Clear();
            _maxWeaponRange.Clear();
        }

        // ── Internal ─────────────────────────────────────────────────────

        private void InsertUnit(IPawn unit)
        {
            InitUnit(unit);
            
            foreach (var u in playerUnits) EvaluateUnit(u, enemyUnits);
            foreach (var u in enemyUnits)  EvaluateUnit(u, playerUnits);
        }
        
        private void InitUnit(IPawn unit)
        {
            _claimedHexes[unit]   = unit.HexPosition;
            _maxWeaponRange[unit] = ResolveMaxRange(unit);
            _controllers[unit]    = new PawnCombatController(unit, _hexGrid, _eventBus, _registry);
        }

        /// <summary>
        /// Core per-unit decision: find a target or move toward one.
        /// Called on combat start, on each hex arrival, and on any defeat.
        /// </summary>
        private void EvaluateUnit(IPawn unit, IReadOnlyList<IPawn> opponents)
        {
            if (!_isRunning) return;

            var target = TargetSelector.Select(unit, opponents, _maxWeaponRange[unit]);
            var controller = _controllers[unit];

            if (LogMovement)
                Debug.Log($"[Move] Evaluate {unit.HexPosition} range={_maxWeaponRange[unit]} " +
                          $"target={(target != null ? target.HexPosition.ToString() : "none")}");

            if (target != null)
            {
                StopMovement(unit);
                controller.SetCurrentTarget(target);
                controller.StartCombat();
            }
            else
            {
                controller.StopCombat();
                StartMovement(unit, opponents);
            }
        }

        private void StartMovement(IPawn unit, IReadOnlyList<IPawn> opponents)
        {
            if (_movementTimers.ContainsKey(unit)) return;

            var nearest = FindNearest(unit, opponents);
            if (nearest == null) return;

            ScheduleNextStep(unit, nearest, opponents);
        }

        private void StopMovement(IPawn unit)
        {
            if (!_movementTimers.TryGetValue(unit, out var timer)) return;
            timer.Stop();
            _movementTimers.Remove(unit);
            _reservedHexes.Remove(unit);
        }

        private void ScheduleNextStep(IPawn unit, IPawn destination, IReadOnlyList<IPawn> opponents)
        {
            var nextHex = ResolveNextHex(unit, destination);

            if (LogMovement)
                Debug.Log($"[Move] Step {unit.HexPosition} -> " +
                          $"{(nextHex == Hex.Invalid ? "INVALID" : nextHex.ToString())} (dest {destination.HexPosition})");

            if (nextHex == Hex.Invalid) return;

            // Reserve before the timer fires — other units see it immediately.
            _reservedHexes[unit] = nextHex;

            var terrainType = _hexGrid.GetTerrain(nextHex);
            var moveCost    = unit.MovementCosts?.GetCost(terrainType) ?? 1;
            var duration    = moveCost / Mathf.Max(unit.Stats.movementSpeed, 0.01f);

            var timer = new Timer(duration, false);
            _movementTimers[unit] = timer;

            // OnComplete (not OnRewind): the step lands after its travel duration, once,
            // driven by the tick loop — never synchronously inside Start().
            timer.OnComplete += () =>
            {
                _movementTimers.Remove(unit);
                _reservedHexes.Remove(unit);

                // A target may have moved into range while this step was travelling.
                // Re-check before committing — otherwise we take a redundant final step.
                if (TargetSelector.Select(unit, opponents, _maxWeaponRange[unit]) != null)
                {
                    EvaluateUnit(unit, opponents); // stops movement and engages
                    return;
                }

                _claimedHexes[unit] = nextHex;
                unit.MoveTo(nextHex);
                EvaluateUnit(unit, opponents);
            };
            timer.Start();
        }

        /// <summary>
        /// Builds invalid and occupied sets, runs A*, and returns the first step hex.
        /// </summary>
        private Hex ResolveNextHex(IPawn unit, IPawn destination)
        {
            var (invalidSet, occupiedSet) = BuildPathingSets(unit);

            var path = HexPathfinder.FindPath(
                unit.HexPosition,
                destination.HexPosition,
                invalidSet,
                _hexGrid,
                occupiedSet,
                unit.MovementCosts);

            if (path == null)
            {
                if (LogMovement)
                    Debug.Log($"[Move]   no path {unit.HexPosition}->{destination.HexPosition} " +
                              $"invalid={invalidSet.Count} occupied={occupiedSet.Count}");
                return Hex.Invalid;
            }

            // Walk the parent chain back to find the first step after the start.
            var node = path;
            while (node.Parent != null && node.Parent.Hex != unit.HexPosition)
                node = node.Parent;

            var firstStep = node.Hex == unit.HexPosition ? Hex.Invalid : node.Hex;

            if (LogMovement)
                Debug.Log($"[Move]   path goalNode={path.Hex} " +
                          $"firstStep={(firstStep == Hex.Invalid ? "INVALID" : firstStep.ToString())} " +
                          $"invalid={invalidSet.Count} occupied={occupiedSet.Count}");

            return firstStep;
        }

        /// <summary>
        /// <para>invalidSet — impassable as destination AND as traversal node (e.g. terrain walls).
        /// Currently only reserved destinations from other mid-movement units.</para>
        /// <para>occupiedSet — high-cost traversal (units currently standing still).
        /// Passable in traversal but never valid as a landing hex.</para>
        /// </summary>
        private (HashSet<Hex> invalidSet, HashSet<Hex> occupiedSet) BuildPathingSets(IPawn movingUnit)
        {
            // Other units' destinations are invalid to land on.
            var invalidSet = new HashSet<Hex>();
            foreach (var (unit, hex) in _reservedHexes)
                if (unit != movingUnit) invalidSet.Add(hex);

            // Units standing still are expensive to pass through, never valid to land on.
            var occupiedSet = new HashSet<Hex>();
            foreach (var (unit, hex) in _claimedHexes)
                if (unit != movingUnit) occupiedSet.Add(hex);

            // Occupied hexes are also invalid as destinations.
            invalidSet.UnionWith(occupiedSet);

            return (invalidSet, occupiedSet);
        }

        private IPawn FindNearest(IPawn unit, IReadOnlyList<IPawn> candidates)
        {
            IPawn nearest  = null;
            var         bestDist = int.MaxValue;

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
            StopMovement(unit);

            if (_controllers.TryGetValue(unit, out var controller))
            {
                controller.StopCombat();
                _controllers.Remove(unit);
            }

            _claimedHexes.Remove(unit);
            _reservedHexes.Remove(unit);
            _maxWeaponRange.Remove(unit);

            _eventBus.PublishDefeated(unit);

            // A team may have just been wiped — end combat instead of re-evaluating survivors.
            if (TryResolveOutcome()) return;

            // Re-evaluate all surviving units — their target may be gone or a new gap opened.
            foreach (var u in playerUnits) EvaluateUnit(u, enemyUnits);
            foreach (var u in enemyUnits)  EvaluateUnit(u, playerUnits);
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
        /// Derives max weapon range from the unit's current chains.
        /// TODO: requires IWeaponItem.Range to be implemented. Defaults to 1 (melee) until then.
        /// </summary>
        private static int ResolveMaxRange(IPawn unit)
        {
            var chains   = ChainResolver.Resolve(unit.Inventory);
            var maxRange = 1;
            foreach (var chain in chains)
                // TODO: revise range!
                if (chain.Weapon.Payload.Range > maxRange)
                    maxRange = chain.Weapon.Payload.Range;
            return maxRange;
        }
    }

    public interface ICombatCoordinator
    {
        event Action<CombatOutcome> OnCombatEnded;

        void StartCombat();
        void StopCombat();
    }
}