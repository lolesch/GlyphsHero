using System;
using UnityEngine;

namespace Code.Runtime.Core.Combat
{
    /// <summary>
    /// Project-side fixed-tick heartbeat for combat resolution (ADR-0001, Decision 6).
    /// Advances on a discrete interval decoupled from <see cref="Time.deltaTime"/>: each
    /// <see cref="Advance"/> banks real elapsed time into an accumulator and fires
    /// <see cref="OnTick"/> once per whole interval (0..N times per call), carrying the
    /// remainder forward. This makes the simulation frame-rate independent — the same total
    /// elapsed time yields the same number of ticks regardless of how it is chunked.
    ///
    /// Pure and engine-agnostic so combat can be unit-tested by feeding deltas directly;
    /// in play the <see cref="CombatCoordinator"/> drives it from its Update loop. This is the
    /// project-side clock the ADR mandates — it deliberately does NOT touch the Utility
    /// Timer/TimerTicker (a submodule whose player-loop driver ticks UI timers too).
    /// </summary>
    public sealed class CombatClock
    {
        private readonly float _tickInterval;
        private readonly int   _maxTicksPerAdvance;
        private float          _accumulator;

        /// <summary>Raised once per elapsed tick interval.</summary>
        public event Action OnTick;

        /// <param name="tickInterval">Seconds per tick (e.g. 0.05 for 20 ticks/sec).</param>
        /// <param name="maxTicksPerAdvance">
        /// Upper bound on ticks fired in a single <see cref="Advance"/> call, guarding against
        /// the "spiral of death" after a long frame hitch. Surplus accumulated time is dropped.
        /// </param>
        public CombatClock(float tickInterval, int maxTicksPerAdvance = 8)
        {
            if (tickInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tickInterval), tickInterval, "Tick interval must be positive.");

            _tickInterval       = tickInterval;
            _maxTicksPerAdvance = Mathf.Max(1, maxTicksPerAdvance);
        }

        public float TickInterval => _tickInterval;

        /// <summary>
        /// Banks <paramref name="deltaTime"/> and fires <see cref="OnTick"/> for every whole
        /// interval now elapsed. Returns the number of ticks fired.
        /// </summary>
        public int Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return 0;

            _accumulator += deltaTime;

            var fired = 0;
            while (_accumulator >= _tickInterval && fired < _maxTicksPerAdvance)
            {
                _accumulator -= _tickInterval;
                fired++;
                OnTick?.Invoke();
            }

            // Drop the backlog beyond the clamp so a hitch can't keep firing on later frames.
            if (_accumulator >= _tickInterval)
                _accumulator = 0f;

            return fired;
        }

        /// <summary>Clears the accumulated remainder. Use when (re)starting combat.</summary>
        public void Reset() => _accumulator = 0f;
    }
}
