using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>Closes an open <see cref="Popup"/> once its <see cref="Popup.PlacementTarget"/> is scrolled or
    /// virtualized out of view — a WPF popup otherwise stays put while its anchor moves away. Call
    /// <c>PopupWatch.Watch(popup)</c> from the popup's <c>Opened</c> handler; the hooks unsubscribe on close.</summary>
    public static class PopupWatch {
        public static void Watch(Popup? popup) {
            if (popup?.PlacementTarget is not FrameworkElement target) return;
            if (Window.GetWindow(target) is not Window wnd) return;

            ScrollChangedEventHandler onScroll = null!;
            DependencyPropertyChangedEventHandler onVis = null!;
            EventHandler onClosed = null!;
            void Cleanup() {
                wnd.RemoveHandler(ScrollViewer.ScrollChangedEvent, onScroll);
                target.IsVisibleChanged -= onVis;
                popup.Closed -= onClosed;
            }
            onScroll = (_, e) => {
                if ((e.VerticalChange != 0 || e.HorizontalChange != 0) && !InView(target)) popup.IsOpen = false;
            };
            onVis = (_, e) => { if (e.NewValue is false) popup.IsOpen = false; };   // hidden/virtualized away
            onClosed = (_, _) => Cleanup();

            wnd.AddHandler(ScrollViewer.ScrollChangedEvent, onScroll, handledEventsToo: true);
            target.IsVisibleChanged += onVis;
            popup.Closed += onClosed;
        }

        // Visible within every ScrollViewer ancestor's viewport (any overlap counts). ActualWidth/Height of the
        // ScrollViewer itself approximates the viewport — ViewportWidth/Height are in ITEMS under item scrolling.
        private static bool InView(FrameworkElement t) {
            if (!t.IsVisible) return false;
            try {
                DependencyObject? p = VisualTreeHelper.GetParent(t);
                while (p != null) {
                    if (p is ScrollViewer sv && sv.ActualWidth > 0) {
                        Rect r = t.TransformToAncestor(sv).TransformBounds(new Rect(0, 0, t.ActualWidth, t.ActualHeight));
                        if (!r.IntersectsWith(new Rect(0, 0, sv.ActualWidth, sv.ActualHeight))) return false;
                    }
                    p = VisualTreeHelper.GetParent(p);
                }
                return true;
            }
            catch (InvalidOperationException) { return false; }   // detached mid-walk (recycled container)
        }
    }
}
