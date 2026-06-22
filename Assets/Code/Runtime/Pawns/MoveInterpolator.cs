using System;
using UnityEngine;

namespace Code.Runtime.Pawns
{
    /// <summary>
    /// View-side glide: eases a world position from <c>from</c> to <c>to</c> over a fixed duration.
    /// Pure (no MonoBehaviour, no Grid) so the step lifecycle and clamping are unit-testable; the
    /// easing shape is injected by the caller (a serialized curve at runtime, a plain function in
    /// tests). The duration is tick-locked by the caller to the <see cref="Core.Combat.CombatClock"/>
    /// interval, so the glide lands exactly as the next tick fires (ADR-0001 lerp polish). Damage and
    /// positioning never read this — the simulation is authoritative on the hex (ADR-0002); this is
    /// cosmetic interpolation between hex states.
    /// </summary>
    public sealed class MoveInterpolator
    {
        private Vector3 _from;
        private Vector3 _to;
        private float   _duration;
        private float   _elapsed;

        public bool    IsMoving { get; private set; }
        public Vector3 Position { get; private set; }

        /// <summary>Start a glide. A zero/negative duration or a no-op move snaps to <paramref name="to"/>.</summary>
        public void Begin(Vector3 from, Vector3 to, float duration)
        {
            _from     = from;
            _to       = to;
            _duration = duration;
            _elapsed  = 0f;

            IsMoving = duration > 0f && from != to;
            Position = IsMoving ? from : to;
        }

        /// <summary>
        /// Advance the glide by <paramref name="dt"/> seconds. <paramref name="ease"/> maps normalized
        /// progress [0,1] to an eased [0,1] (overshoot allowed). Clamps and stops on arrival.
        /// </summary>
        public Vector3 Advance(float dt, Func<float, float> ease)
        {
            if (!IsMoving) return Position;

            _elapsed += dt;
            var t = Mathf.Clamp01(_elapsed / _duration);
            Position = Vector3.LerpUnclamped(_from, _to, ease(t));

            if (t >= 1f)
            {
                IsMoving = false;
                Position = _to;
            }

            return Position;
        }
    }
}
