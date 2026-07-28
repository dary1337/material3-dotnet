using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>A wrapping panel that VIRTUALIZES — WPF ships none, and <see cref="WrapPanel"/> realizes every
    /// item. Uniform cell size, left-to-right top-to-bottom, vertical scrolling only, realizing just the rows in
    /// view plus one row of overscan. Use as an <c>ItemsPanelTemplate</c> and set <see cref="ItemWidth"/> /
    /// <see cref="ItemHeight"/> to the cell size (item + margins).</summary>
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo {
        // Set per-usage so columns and extent are right from the FIRST pass; auto-detecting from the first child
        // mis-sizes the cell and clips items. 0 => fall back to auto-detect.
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
            nameof(ItemWidth), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public double ItemWidth { get => (double)GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }
        public double ItemHeight { get => (double)GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

        private Size _itemSize = new Size(172, 162);   // seed; overridden by ItemWidth/ItemHeight or first child
        private Size _extent;
        private Size _viewport;
        private Point _offset;

        private int Columns => Math.Max(1, (int)Math.Floor(_viewport.Width / _itemSize.Width));

        private bool FixedSize => ItemWidth > 0 && ItemHeight > 0;

        public VirtualizingWrapPanel() {
            // The first measure after Collapsed->Visible realizes containers but they don't paint until a SECOND
            // pass, leaving a blank grid until the user scrolls. Force that pass on becoming visible.
            IsVisibleChanged += (_, e) => {
                if (e.NewValue is bool v && v)
                    Dispatcher.BeginInvoke(new Action(InvalidateMeasure), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        protected override Size MeasureOverride(Size availableSize) {
            if (FixedSize) _itemSize = new Size(ItemWidth, ItemHeight);
            ItemsControl? owner = ItemsControl.GetItemsOwner(this);
            int count = owner?.Items.Count ?? 0;
            UpdateScrollInfo(availableSize, count);

            (int first, int last) = VisibleRange(_offset.Y, _viewport.Height, _viewport.Width, _itemSize.Width, _itemSize.Height, count);

            var generator = ItemContainerGenerator;
            if (generator == null) return availableSize;

            GeneratorPosition startPos = generator.GeneratorPositionFromIndex(first);
            int childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;
            using (generator.StartAt(startPos, GeneratorDirection.Forward, true)) {
                for (int i = first; i <= last && i < count; i++, childIndex++) {
                    UIElement child = (UIElement)generator.GenerateNext(out bool newlyRealized);
                    if (newlyRealized) {
                        if (childIndex >= InternalChildren.Count) AddInternalChild(child);
                        else InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    child.Measure(new Size(_itemSize.Width, _itemSize.Height));
                    if (!FixedSize && child.DesiredSize.Width > 1 && child.DesiredSize.Height > 1)
                        _itemSize = child.DesiredSize;
                }
            }
            CleanUpChildren(first, last);

            return new Size(
                double.IsInfinity(availableSize.Width) ? _extent.Width : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? _extent.Height : availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            int cols = Columns;
            var generator = ItemContainerGenerator;
            for (int vi = 0; vi < InternalChildren.Count; vi++) {
                UIElement child = InternalChildren[vi];
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(vi, 0));
                if (itemIndex < 0) continue;
                int row = itemIndex / cols, col = itemIndex % cols;
                child.Arrange(new Rect(col * _itemSize.Width - _offset.X, row * _itemSize.Height - _offset.Y,
                    _itemSize.Width, _itemSize.Height));
            }
            return finalSize;
        }

        // A virtualizing panel MUST drop the realized container when its item leaves the source, or the generator's
        // map desyncs and the next Remove throws an NRE.
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args) {
            switch (args.Action) {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveInternalChildRange(0, InternalChildren.Count);
                    break;
            }
            InvalidateMeasure();
        }

        private void CleanUpChildren(int first, int last) {
            var generator = ItemContainerGenerator;
            for (int i = InternalChildren.Count - 1; i >= 0; i--) {
                var pos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(pos);
                if (itemIndex < first || itemIndex > last) {
                    generator.Remove(pos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        /// <summary>The inclusive item-index range to realize for a scroll offset, plus one row of overscan each
        /// way. Pure (no WPF state) so it can be unit-tested; <c>last &lt; first</c> means "nothing to realize".</summary>
        public static (int first, int last) VisibleRange(double offsetY, double viewportH, double viewportW,
                                                         double itemW, double itemH, int count) {
            if (count <= 0 || itemW <= 0 || itemH <= 0) return (0, -1);
            int cols = Math.Max(1, (int)Math.Floor(viewportW / itemW));
            int firstRow = Math.Max(0, (int)Math.Floor(offsetY / itemH) - 1);
            int lastRow = (int)Math.Ceiling((offsetY + viewportH) / itemH);
            int first = Math.Max(0, firstRow * cols);
            int last = Math.Min(count - 1, (lastRow + 1) * cols - 1);
            return (first, last);
        }

        private void UpdateScrollInfo(Size available, int count) {
            int cols = Math.Max(1, (int)Math.Floor(available.Width / _itemSize.Width));
            int rows = (int)Math.Ceiling((double)count / cols);
            var extent = new Size(cols * _itemSize.Width, rows * _itemSize.Height);
            if (extent != _extent) { _extent = extent; ScrollOwner?.InvalidateScrollInfo(); }
            if (available != _viewport) { _viewport = available; ScrollOwner?.InvalidateScrollInfo(); }
            double maxY = Math.Max(0, _extent.Height - _viewport.Height);
            if (_offset.Y > maxY) SetVerticalOffset(maxY);
        }

        // ---- IScrollInfo (vertical) ----
        public ScrollViewer? ScrollOwner { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public double ExtentWidth => _extent.Width;
        public double ExtentHeight => _extent.Height;
        public double ViewportWidth => _viewport.Width;
        public double ViewportHeight => _viewport.Height;
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;

        private const double Line = 48;
        // Wheel scrolls ~one cell row per notch (cells are tall); line/page keys use smaller/full steps.
        private double WheelStep => Math.Max(Line, _itemSize.Height);
        public void LineUp() => SetVerticalOffset(_offset.Y - Line);
        public void LineDown() => SetVerticalOffset(_offset.Y + Line);
        public void WheelUp() => SetVerticalOffset(_offset.Y - WheelStep);
        public void WheelDown() => SetVerticalOffset(_offset.Y + WheelStep);
        public void PageUp() => SetVerticalOffset(_offset.Y - _viewport.Height);
        public void PageDown() => SetVerticalOffset(_offset.Y + _viewport.Height);
        public void LineLeft() { }
        public void LineRight() { }
        public void WheelLeft() { }
        public void WheelRight() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void MouseWheelUp() => WheelUp();
        public void MouseWheelDown() => WheelDown();
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }
        public void SetHorizontalOffset(double offset) { }

        public void SetVerticalOffset(double offset) {
            double maxY = Math.Max(0, _extent.Height - _viewport.Height);
            double y = Math.Max(0, Math.Min(offset, maxY));
            if (Math.Abs(y - _offset.Y) < 0.001) return;
            _offset.Y = y;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) {
            for (int i = 0; i < InternalChildren.Count; i++) {
                if (!ReferenceEquals(InternalChildren[i], visual)) continue;
                int itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
                if (itemIndex < 0) break;
                int row = itemIndex / Columns;
                double top = row * _itemSize.Height, bottom = top + _itemSize.Height;
                if (top < _offset.Y) SetVerticalOffset(top);
                else if (bottom > _offset.Y + _viewport.Height) SetVerticalOffset(bottom - _viewport.Height);
                break;
            }
            return rectangle;
        }
    }
}
