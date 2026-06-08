using System;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Drawing {
    /// <summary>Shared exponential-ease scalar behind every owner-drawn control's tween (slider handle,
    /// switch thumb, progress fill, check mark, floating label). Snaps instead of easing when
    /// <see cref="Motion.AnimationsEnabled"/> or <see cref="Animated"/> is off, or the owner has no handle.</summary>
    internal sealed class AnimatedValue : IDisposable {
        private readonly Control _owner;
        private readonly float _factor;
        private readonly float _threshold;
        private readonly Timer _timer;
        private float _current;
        private float _target;
        private bool _animated = true;

        /// <param name="owner">Control invalidated on every frame as the value eases.</param>
        /// <param name="factor">Fraction of the remaining distance closed per frame (e.g. 0.25 ≈ M3 short4 at 60fps).</param>
        /// <param name="threshold">Distance to the target at which the glide snaps and stops.</param>
        /// <param name="intervalMs">Frame interval; 16 ms ≈ 60fps.</param>
        public AnimatedValue(Control owner, float factor, float threshold, int intervalMs = 16) {
            _owner = owner;
            _factor = factor;
            _threshold = threshold;
            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        /// <summary>The current eased value — read this in OnPaint.</summary>
        public float Current => _current;

        /// <summary>Per-instance switch on top of the global <see cref="Motion.AnimationsEnabled"/>; setting it false snaps.</summary>
        public bool Animated {
            get => _animated;
            set {
                _animated = value;
                if (!value) {
                    Snap();
                }
            }
        }

        /// <summary>Eases toward <paramref name="target"/>, or snaps if animation is off, no handle yet, or already within threshold.</summary>
        public void To(float target) {
            _target = target;
            if (!_animated || !Motion.AnimationsEnabled || !_owner.IsHandleCreated
                    || Math.Abs(_target - _current) < _threshold) {
                Snap();
            }
            else if (!_timer.Enabled) {
                _timer.Start();
            }
        }

        /// <summary>Sets the target and jumps to it (design-time or a forced initial value).</summary>
        public void SnapTo(float target) {
            _target = target;
            Snap();
        }

        private void Snap() {
            _current = _target;
            _timer.Stop();
            if (!_owner.IsDisposed) {
                _owner.Invalidate();
            }
        }

        private void OnTick(object? sender, EventArgs e) {
            // Re-checked each frame so turning the flag off snaps an in-flight glide too.
            float delta = _target - _current;
            if (!_animated || !Motion.AnimationsEnabled || Math.Abs(delta) < _threshold) {
                _current = _target;
                _timer.Stop();
            }
            else {
                _current += delta * _factor;
            }
            if (!_owner.IsDisposed) {
                _owner.Invalidate();
            }
        }

        public void Dispose() {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
