using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>
    /// Makes the mouse wheel step every pixel-scrolling <see cref="ScrollViewer"/> by the system's wheel setting
    /// at a browser's line height. Opt in once at startup with <see cref="Register"/>.
    /// </summary>
    /// <remarks>
    /// WPF scrolls a fixed 16 px per wheel line, so the same Windows setting feels half as fast as in a browser.
    /// A viewer that scrolls by item (a virtualizing stack) is left to WPF, whose offsets there count items rather
    /// than pixels; one hosting a <see cref="VirtualizingWrapPanel"/> scrolls by pixel and is included. A viewer with
    /// nothing to scroll lets the wheel through to the page around it.
    /// </remarks>
    public static class WheelScroll {
        /** Chromium scrolls 100 px for the default three lines. */
        private const double LinePx = 100.0 / 3;
        private const double NotchDelta = 120;

        private static bool _registered;

        /// <summary>Installs the class handler for every <see cref="ScrollViewer"/> in the app. Idempotent.</summary>
        public static void Register() {
            if (_registered) return;
            _registered = true;
            EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseWheelEvent, new MouseWheelEventHandler(OnWheel));
        }

        internal static double Step(int delta, int wheelLines, double viewportHeight) =>
            delta / NotchDelta * (wheelLines < 0 ? viewportHeight : wheelLines * LinePx);

        private static void OnWheel(object sender, MouseWheelEventArgs e) {
            if (e.Handled || !(sender is ScrollViewer sv)) return;
            if (sv.CanContentScroll && !(ItemsHost(sv) is VirtualizingWrapPanel)) return;
            if (sv.ScrollableHeight <= 0) return;
            double step = Step(e.Delta, SystemParameters.WheelScrollLines, sv.ViewportHeight);
            double basis = Pending.TryGetValue(sv, out Request? last) && sv.VerticalOffset == last.From ? last.Target : sv.VerticalOffset;
            double target = Math.Max(0, Math.Min(sv.ScrollableHeight, basis - step));
            Pending.Remove(sv);
            Pending.Add(sv, new Request { From = sv.VerticalOffset, Target = target });
            sv.ScrollToVerticalOffset(target);
            e.Handled = true;
        }

        private static Panel? ItemsHost(ScrollViewer sv) {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(sv);
            while (queue.Count > 0) {
                DependencyObject d = queue.Dequeue();
                if (d is Panel p && p.IsItemsHost) return p;
                if (d is ScrollViewer inner && !ReferenceEquals(inner, sv)) continue;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++) queue.Enqueue(VisualTreeHelper.GetChild(d, i));
            }
            return null;
        }

        private sealed class Request {
            public double From, Target;
        }

        private static readonly ConditionalWeakTable<ScrollViewer, Request> Pending = new ConditionalWeakTable<ScrollViewer, Request>();
    }
}
