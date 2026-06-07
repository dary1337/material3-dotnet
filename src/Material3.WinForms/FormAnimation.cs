using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms {
    /// <summary>Material 3 open/close motion for modal dialogs: top-down slide combined with opacity fade.</summary>
    public static class FormAnimation {
        public const int OpenDurationMs = Motion.Short4;
        public const int CloseDurationMs = 140;
        private const int FrameDelayMs = 8;
        private const int SlideOffsetPx = 10;

        // Bumping the epoch makes any in-flight loop bail, so a concurrent open and close can't
        // fight over Location/Opacity.
        private static readonly ConditionalWeakTable<Form, StrongBox<int>> Epochs =
            new ConditionalWeakTable<Form, StrongBox<int>>();

        private static int BeginEpoch(Form form) {
            StrongBox<int> box = Epochs.GetValue(form, _ => new StrongBox<int>(0));
            return ++box.Value;
        }

        private static bool IsCurrent(Form form, int epoch) =>
            Epochs.TryGetValue(form, out StrongBox<int>? box) && box.Value == epoch;

        public static async Task OpenAsync(Form form) {
            if (form == null || form.IsDisposed) {
                return;
            }
            int epoch = BeginEpoch(form);
            Point target = form.Location;
            Point start = new Point(target.X, target.Y - SlideOffsetPx);
            form.Location = start;
            form.Opacity = 0d;

            var sw = Stopwatch.StartNew();
            while (true) {
                if (form.IsDisposed || !IsCurrent(form, epoch)) {
                    return;
                }
                double t = Math.Min(1.0, sw.ElapsedMilliseconds / (double)OpenDurationMs);
                double eased = Motion.EmphasizedDecelerate.Evaluate(t);
                form.Opacity = eased;
                form.Location = new Point(
                    target.X,
                    start.Y + (int)Math.Round((target.Y - start.Y) * eased)
                );
                if (t >= 1.0) {
                    break;
                }
                await Task.Delay(FrameDelayMs);
            }
            if (!form.IsDisposed && IsCurrent(form, epoch)) {
                form.Opacity = 1d;
                form.Location = target;
            }
        }

        public static async Task CloseAsync(Form form) {
            if (form == null || form.IsDisposed) {
                return;
            }
            int epoch = BeginEpoch(form);
            Point start = form.Location;
            Point target = new Point(start.X, start.Y + SlideOffsetPx);
            double startOpacity = form.Opacity;

            var sw = Stopwatch.StartNew();
            while (true) {
                if (form.IsDisposed || !IsCurrent(form, epoch)) {
                    return;
                }
                double t = Math.Min(1.0, sw.ElapsedMilliseconds / (double)CloseDurationMs);
                double eased = Motion.EmphasizedAccelerate.Evaluate(t);
                form.Opacity = startOpacity * (1d - eased);
                form.Location = new Point(
                    start.X,
                    start.Y + (int)Math.Round((target.Y - start.Y) * eased)
                );
                if (t >= 1.0) {
                    break;
                }
                await Task.Delay(FrameDelayMs);
            }
            if (!form.IsDisposed && IsCurrent(form, epoch)) {
                form.Opacity = 0d;
            }
        }
    }
}
