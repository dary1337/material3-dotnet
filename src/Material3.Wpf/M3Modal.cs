using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>Per-modal behaviour for <see cref="M3Modal.Show"/>.</summary>
    public sealed class ModalOptions {
        /// <summary>Darken the scrim (M3 <c>Scrim</c> role). When false the scrim is transparent but still
        /// blocks input, so the modal is enforced without dimming.</summary>
        public bool Dim { get; set; } = true;
        public bool DismissOnScrimClick { get; set; } = true;
        public bool DismissOnEsc { get; set; } = true;
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;
        /// <summary>Called when the scrim/Esc requests a dismiss. If set, the modal is NOT auto-closed — the app
        /// is expected to close it (e.g. by clearing a bound "open" flag), so a data-driven modal stays in sync.</summary>
        public Action? OnDismiss { get; set; }
        /// <summary>Called after the content has been removed from the host (post close-animation) — restore
        /// state or re-home the element here.</summary>
        public Action? OnClosed { get; set; }
    }

    /// <summary>Handle to an open modal; call <see cref="Close"/> to dismiss it.</summary>
    public interface IModalHandle { void Close(); }

    /// <summary>
    /// Drop ONE of these as the outermost element of a window, wrapping all of its content. Modals shown with
    /// <see cref="M3Modal"/> render above a scrim that covers the whole layer (nav, chrome and content), so an
    /// open modal blocks everything behind it — solving the "a view-local scrim can't cover the app chrome"
    /// problem. Reference the enclosing layer is automatic; just call <c>M3Modal.Show(card)</c>.
    /// </summary>
    public class M3ModalLayer : ContentControl {
        internal static M3ModalLayer? Active { get; private set; }
        internal Border? Scrim { get; private set; }
        internal Panel? ModalHost { get; private set; }

        /// <summary>Inset applied to the scrim and modal host so an open modal doesn't cover a custom window
        /// chrome (title bar / caption buttons). E.g. <c>0,40,0,0</c> keeps the top 40px caption clickable.</summary>
        public Thickness ChromeInset {
            get => (Thickness)GetValue(ChromeInsetProperty);
            set => SetValue(ChromeInsetProperty, value);
        }
        public static readonly DependencyProperty ChromeInsetProperty =
            DependencyProperty.Register(nameof(ChromeInset), typeof(Thickness), typeof(M3ModalLayer),
                new PropertyMetadata(default(Thickness)));

        public M3ModalLayer() {
            Loaded += (_, __) => Active = this;
            Unloaded += (_, __) => { if (ReferenceEquals(Active, this)) Active = null; };
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            Scrim = GetTemplateChild("PART_Scrim") as Border;
            ModalHost = GetTemplateChild("PART_ModalHost") as Panel;
            if (Scrim != null) Scrim.MouseLeftButtonDown += (_, __) => M3Modal.DismissTop(scrimClick: true);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape && M3Modal.HasModal && M3Modal.DismissTop(esc: true)) e.Handled = true;
        }
    }

    /// <summary>App-level modal service. Renders a <see cref="FrameworkElement"/> above a shared scrim inside the
    /// enclosing <see cref="M3ModalLayer"/>, ref-counted so stacked modals share one scrim.</summary>
    public static class M3Modal {
        private sealed class Entry : IModalHandle {
            public FrameworkElement Content = null!;
            public ModalOptions Options = null!;
            public M3ModalLayer Layer = null!;
            public IInputElement? PrevFocus;
            public void Close() => CloseEntry(this);
        }

        private static readonly List<Entry> Stack = new List<Entry>();

        public static bool HasModal => Stack.Count > 0;

        public static IModalHandle Show(FrameworkElement content, ModalOptions? options = null) {
            M3ModalLayer layer = M3ModalLayer.Active
                ?? throw new InvalidOperationException("No M3ModalLayer in the tree — wrap the window content in <m3:M3ModalLayer>.");
            if (layer.ModalHost == null || layer.Scrim == null) layer.ApplyTemplate();
            ModalOptions opts = options ?? new ModalOptions();

            var e = new Entry { Content = content, Options = opts, Layer = layer, PrevFocus = Keyboard.FocusedElement };
            content.HorizontalAlignment = opts.HorizontalAlignment;
            content.VerticalAlignment = opts.VerticalAlignment;
            KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);   // trap Tab inside the modal
            layer.ModalHost!.Children.Add(content);
            Stack.Add(e);

            layer.Scrim!.Background = opts.Dim ? ScrimBrush(layer) : Brushes.Transparent;
            Motion.OpenModal(layer.Scrim, content);
            content.Dispatcher.BeginInvoke(new Action(() => content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))),
                System.Windows.Threading.DispatcherPriority.Input);
            return e;
        }

        internal static bool DismissTop(bool scrimClick = false, bool esc = false) {
            if (Stack.Count == 0) return false;
            Entry top = Stack[Stack.Count - 1];
            if (scrimClick && !top.Options.DismissOnScrimClick) return false;
            if (esc && !top.Options.DismissOnEsc) return false;
            if (top.Options.OnDismiss != null) { top.Options.OnDismiss(); return true; }   // app drives the close
            CloseEntry(top);
            return true;
        }

        private static void CloseEntry(Entry e) {
            if (!Stack.Remove(e)) return;
            M3ModalLayer layer = e.Layer;
            void RemoveAndRestore() {
                layer.ModalHost?.Children.Remove(e.Content);
                if (e.PrevFocus != null) Keyboard.Focus(e.PrevFocus);
                e.Options.OnClosed?.Invoke();
            }
            if (Stack.Count == 0) Motion.CloseModal(layer.Scrim!, e.Content, RemoveAndRestore);   // last one out → fade scrim + scale the card down
            else RemoveAndRestore();                                                    // scrim stays for the modals below
        }

        private static Brush ScrimBrush(FrameworkElement fe) =>
            fe.TryFindResource("Scrim") as Brush ?? new SolidColorBrush(Color.FromArgb(168, 0, 0, 0));
    }
}
