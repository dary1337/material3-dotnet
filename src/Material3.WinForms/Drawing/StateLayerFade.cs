using System;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Drawing {
    internal sealed class StateLayerFade : IDisposable {
        private const float Factor = 0.35f;
        private const float Threshold = 0.002f;
        private readonly AnimatedValue _value;
        private float _target;

        public StateLayerFade(Control owner) {
            _value = Create(owner);
        }

        // Called from OnPaint; the target is re-set only when the state changes, or a snap would repaint forever.
        public double Resolve(bool hovered, bool pressed, bool enabled = true) =>
            Resolve(hovered, pressed, enabled, StateLayers.Hover, StateLayers.Pressed);

        public double Resolve(bool hovered, bool pressed, bool enabled, double hover, double press) {
            float target = !enabled ? 0f : (float)(pressed ? press : hovered ? hover : 0d);
            if (target != _target) {
                _target = target;
                _value.To(target);
            }
            return _value.Current;
        }

        internal static AnimatedValue Create(Control owner) => new AnimatedValue(owner, Factor, Threshold);

        public void Dispose() => _value.Dispose();
    }

    internal sealed class ItemHoverFade : IDisposable {
        private readonly AnimatedValue _in;
        private readonly AnimatedValue _out;
        private int _hot = -1;
        private int _leaving = -1;

        public ItemHoverFade(Control owner) {
            _in = StateLayerFade.Create(owner);
            _out = StateLayerFade.Create(owner);
        }

        public void SetHot(int index) {
            if (index == _hot) {
                return;
            }

            _leaving = _hot;
            _out.SnapTo(_in.Current);
            _out.To(0f);
            _hot = index;
            _in.SnapTo(0f);
            if (index >= 0) {
                _in.To((float)StateLayers.Hover);
            }
        }

        public double Opacity(int index) =>
            index == _hot ? _in.Current : index == _leaving ? _out.Current : 0d;

        public void Dispose() {
            _in.Dispose();
            _out.Dispose();
        }
    }
}
