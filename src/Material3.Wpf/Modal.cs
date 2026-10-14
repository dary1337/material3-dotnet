using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>Per-modal behaviour for <see cref="Modal"/>.</summary>
    public sealed class ModalOptions {
        /// <summary>Darken the scrim (M3 <c>Scrim</c> role). When false the scrim is transparent but still
        /// blocks input, so the modal is enforced without dimming.</summary>
        public bool Dim { get; set; } = true;
        /// <summary>Whether a click on the scrim dismisses the modal. Read at the moment of the click, so it can change while the modal is up.</summary>
        public bool DismissOnScrimClick { get; set; } = true;
        /// <summary>Whether Esc dismisses the modal. Read at the moment of the key press, so it can change while the modal is up.</summary>
        public bool DismissOnEsc { get; set; } = true;
        /// <summary>Where the content sits across the layer.</summary>
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;
        /// <summary>Where the content sits down the layer.</summary>
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;
        /// <summary>Called when the scrim/Esc requests a dismiss. If set, the modal is NOT auto-closed — the app
        /// is expected to close it (e.g. by clearing a bound "open" flag), so a data-driven modal stays in sync.</summary>
        public Action? DismissHandler { get; set; }
        /// <summary>Called after the content has been removed from the host (post close-animation) — restore
        /// state or re-home the element here.</summary>
        public Action? ClosedHandler { get; set; }
    }

    /// <summary>Handle to an open modal; call <see cref="Close"/> to dismiss it.</summary>
    public interface IModalHandle {
        /// <summary>Plays the exit and then removes the content.</summary>
        void Close();
    }

    /// <summary>
    /// Drop ONE of these as the outermost element of a window, wrapping all of its content. Modals shown with
    /// <see cref="Modal"/> render above a scrim that covers the whole layer (nav, chrome and content), so an
    /// open modal blocks everything behind it — solving the "a view-local scrim can't cover the app chrome"
    /// problem. Reference the enclosing layer is automatic; just call <c>Modal.Show(card)</c>.
    /// </summary>
    public class ModalLayer : ContentControl {
        [ThreadStatic] private static List<ModalLayer>? _loaded;
        [ThreadStatic] private static ModalLayer? _active;

        private static List<ModalLayer> Loaded_ => _loaded ??= new List<ModalLayer>();
        internal static ModalLayer? Active { get => _active; private set => _active = value; }
        internal static IReadOnlyList<ModalLayer> All => Loaded_;
        internal Border? Scrim { get; private set; }
        internal Panel? ModalHost { get; private set; }
        internal readonly List<Modal.Entry> Entries = new List<Modal.Entry>();
        internal Action? PendingClose;

        /// <summary>Inset applied to the scrim and modal host so an open modal doesn't cover a custom window
        /// chrome (title bar / caption buttons). E.g. <c>0,40,0,0</c> keeps the top 40px caption clickable.</summary>
        public Thickness ChromeInset {
            get => (Thickness)GetValue(ChromeInsetProperty);
            set => SetValue(ChromeInsetProperty, value);
        }
        /// <summary>Identifies the <see cref="ChromeInset"/> dependency property.</summary>
        public static readonly DependencyProperty ChromeInsetProperty =
            DependencyProperty.Register(nameof(ChromeInset), typeof(Thickness), typeof(ModalLayer),
                new PropertyMetadata(default(Thickness)));

        /// <summary>Initializes a new instance of the <see cref="ModalLayer"/> class.</summary>
        public ModalLayer() {
            Loaded += (_, __) => {
                if (!Loaded_.Contains(this)) Loaded_.Add(this);
                Active = this;
            };
            Unloaded += (_, __) => {
                Loaded_.Remove(this);
                if (ReferenceEquals(Active, this)) Active = Loaded_.Count > 0 ? Loaded_[Loaded_.Count - 1] : null;
            };
        }

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            Scrim = GetTemplateChild("PART_Scrim") as Border;
            ModalHost = GetTemplateChild("PART_ModalHost") as Panel;
            if (Scrim != null) Scrim.MouseLeftButtonDown += (_, __) => Modal.DismissTop(this, scrimClick: true);
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape && Entries.Count > 0 && Modal.DismissTop(this, esc: true)) e.Handled = true;
        }
    }

    /// <summary>
    /// Window-level modal service. Renders a <see cref="FrameworkElement"/> above a scrim inside a window's
    /// <see cref="ModalLayer"/>; stacked modals in one window share its scrim, and each window keeps its own stack.
    /// </summary>
    public static class Modal {
        internal sealed class Entry : IModalHandle {
            public FrameworkElement Content = null!;
            public ModalOptions Options = null!;
            public ModalLayer Layer = null!;
            public IInputElement? PrevFocus;
            public object? PrevHAlign; public object? PrevVAlign; public object? PrevTabNav;
            public void Close() => CloseEntry(this);
        }

        /// <summary>Whether a modal is showing in any window of the calling UI thread.</summary>
        public static bool HasModal => ModalLayer.All.Any(layer => layer.Entries.Count > 0);

        /// <summary>
        /// Shows <paramref name="content"/> over the <see cref="ModalLayer"/> of the active window — or of the most
        /// recently loaded one when none of the app's windows is active — and returns the handle that closes it.
        /// </summary>
        public static IModalHandle Show(FrameworkElement content, ModalOptions? options = null) =>
            ShowIn(PickLayer(null), content, options);

        /// <summary>
        /// Shows <paramref name="content"/> over the <see cref="ModalLayer"/> of the window that holds
        /// <paramref name="owner"/> — a window, or any element inside one.
        /// </summary>
        public static IModalHandle Show(DependencyObject owner, FrameworkElement content, ModalOptions? options = null) =>
            ShowIn(PickLayer(owner ?? throw new ArgumentNullException(nameof(owner))), content, options);

        internal static ModalLayer? LayerOf(DependencyObject owner) {
            for (DependencyObject? d = owner; d != null; d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d))
                if (d is ModalLayer layer) return layer;
            Window? window = owner as Window ?? Window.GetWindow(owner);
            return window == null ? null : ModalLayer.All.LastOrDefault(l => ReferenceEquals(Window.GetWindow(l), window));
        }

        private static ModalLayer PickLayer(DependencyObject? owner) {
            ModalLayer? layer = owner != null
                ? LayerOf(owner)
                : ModalLayer.All.LastOrDefault(l => Window.GetWindow(l)?.IsActive == true) ?? ModalLayer.Active;
            return layer ?? throw new InvalidOperationException("No ModalLayer in the tree — wrap the window content in <m3:ModalLayer>.");
        }

        private static IModalHandle ShowIn(ModalLayer layer, FrameworkElement content, ModalOptions? options) {
            if (layer.ModalHost == null || layer.Scrim == null) layer.ApplyTemplate();
            if (layer.ModalHost == null || layer.Scrim == null)
                throw new InvalidOperationException("ModalLayer template is missing PART_ModalHost / PART_Scrim.");
            ModalOptions opts = options ?? new ModalOptions();
            Action? pending = layer.PendingClose; layer.PendingClose = null; pending?.Invoke();
            if (ModalLayer.All.Any(l => l.Entries.Exists(x => ReferenceEquals(x.Content, content))))
                throw new InvalidOperationException("Modal.Show: this content is already open as a modal.");
            if (VisualTreeHelper.GetParent(content) != null)
                throw new InvalidOperationException("Modal.Show: content already has a parent — remove it from its current host first.");

            var e = new Entry {
                Content = content, Options = opts, Layer = layer, PrevFocus = Keyboard.FocusedElement,
                PrevHAlign = content.ReadLocalValue(FrameworkElement.HorizontalAlignmentProperty),
                PrevVAlign = content.ReadLocalValue(FrameworkElement.VerticalAlignmentProperty),
                PrevTabNav = content.ReadLocalValue(KeyboardNavigation.TabNavigationProperty),
            };
            content.HorizontalAlignment = opts.HorizontalAlignment;
            content.VerticalAlignment = opts.VerticalAlignment;
            KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);
            layer.ModalHost!.Children.Add(content);
            bool first = layer.Entries.Count == 0;
            layer.Entries.Add(e);

            ApplyScrim(layer, opts.Dim);
            if (first) Motion.OpenModal(layer.Scrim!, content);
            else Motion.Pop(content);
            content.Dispatcher.BeginInvoke(new Action(() => content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))),
                System.Windows.Threading.DispatcherPriority.Input);
            return e;
        }

        internal static bool DismissTop(ModalLayer layer, bool scrimClick = false, bool esc = false) {
            if (layer.Entries.Count == 0) return false;
            Entry top = layer.Entries[layer.Entries.Count - 1];
            if (scrimClick && !top.Options.DismissOnScrimClick) return false;
            if (esc && !top.Options.DismissOnEsc) return false;
            if (top.Options.DismissHandler != null) { top.Options.DismissHandler(); return true; }
            CloseEntry(top);
            return true;
        }

        private static void CloseEntry(Entry e) {
            ModalLayer layer = e.Layer;
            int index = layer.Entries.IndexOf(e);
            if (index < 0) return;
            bool wasTop = index == layer.Entries.Count - 1;
            layer.Entries.RemoveAt(index);
            void RemoveAndRestore() {
                layer.ModalHost?.Children.Remove(e.Content);
                Restore(e.Content, FrameworkElement.HorizontalAlignmentProperty, e.PrevHAlign);
                Restore(e.Content, FrameworkElement.VerticalAlignmentProperty, e.PrevVAlign);
                Restore(e.Content, KeyboardNavigation.TabNavigationProperty, e.PrevTabNav);
                if (wasTop && e.PrevFocus != null) Keyboard.Focus(e.PrevFocus);
                e.Options.ClosedHandler?.Invoke();
            }
            if (layer.Entries.Count == 0) {
                bool done = false;
                Action? finish = null;
                void Once() {
                    if (done) return;
                    done = true;
                    if (ReferenceEquals(layer.PendingClose, finish)) layer.PendingClose = null;
                    RemoveAndRestore();
                }
                finish = Motion.CloseModal(layer.Scrim!, e.Content, Once);
                layer.PendingClose = finish;
            }
            else {
                RemoveAndRestore();
                ApplyScrim(layer, layer.Entries[layer.Entries.Count - 1].Options.Dim);
            }
        }

        private static void ApplyScrim(ModalLayer layer, bool dim) {
            if (layer.Scrim == null) return;
            if (!dim) layer.Scrim.Background = Brushes.Transparent;
            else if (layer.TryFindResource("Scrim") is Brush) layer.Scrim.SetResourceReference(Border.BackgroundProperty, "Scrim");
            else layer.Scrim.Background = new SolidColorBrush(Color.FromArgb(168, 0, 0, 0));
        }

        private static void Restore(FrameworkElement el, DependencyProperty prop, object? prev) {
            if (prev == DependencyProperty.UnsetValue) el.ClearValue(prop);
            else if (prev is System.Windows.Data.BindingExpressionBase b) System.Windows.Data.BindingOperations.SetBinding(el, prop, b.ParentBindingBase);
            else el.SetValue(prop, prev!);
        }
    }
}