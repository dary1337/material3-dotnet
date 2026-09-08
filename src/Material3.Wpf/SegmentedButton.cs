using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>Where a segment sits in the row.</summary>
    public enum SegmentPosition {
        /// <summary>Between two others: square on both sides.</summary>
        Middle,
        /// <summary>First of several: rounded on the left.</summary>
        First,
        /// <summary>Last of several: rounded on the right.</summary>
        Last,
        /// <summary>The only segment: rounded on both sides.</summary>
        Only,
    }

    /// <summary>One segment of a <see cref="SegmentedButton"/>. Generated for you — items can be plain strings.</summary>
    public class SegmentedItem : ToggleButton {
        static SegmentedItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentedItem), new FrameworkPropertyMetadata(typeof(SegmentedItem)));
        }

        internal static readonly DependencyPropertyKey PositionKey = DependencyProperty.RegisterReadOnly(
            "Position", typeof(SegmentPosition), typeof(SegmentedItem), new PropertyMetadata(SegmentPosition.Middle));

        /// <summary>Identifies the <see cref="Position"/> property.</summary>
        public static readonly DependencyProperty PositionProperty = PositionKey.DependencyProperty;

        /// <summary>Where the segment sits in the row — the template rounds only the outer edges.</summary>
        public SegmentPosition Position => (SegmentPosition)GetValue(PositionProperty);
    }

    /// <summary>
    /// A row of segments that share one outline and act as one control — the choice between a handful of
    /// mutually exclusive views, where a dropdown would hide the options and radios would cost a column.
    /// </summary>
    /// <remarks>
    /// Selection lives on the row, not on the segments: with <see cref="MultiSelect"/> off, checking one
    /// unchecks the rest, so no caller has to keep a group in sync.
    /// </remarks>
    public class SegmentedButton : ItemsControl {
        static SegmentedButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentedButton), new FrameworkPropertyMetadata(typeof(SegmentedButton)));
        }

        /// <summary>Raised after the selection changes, whoever changed it.</summary>
        public event EventHandler? SelectionChanged;

        /// <summary>Identifies the <see cref="MultiSelect"/> property.</summary>
        public static readonly DependencyProperty MultiSelectProperty = DependencyProperty.Register(
            nameof(MultiSelect), typeof(bool), typeof(SegmentedButton), new PropertyMetadata(false));

        /// <summary>Whether several segments can be on at once. Off by default — one choice, like a radio group.</summary>
        public bool MultiSelect { get => (bool)GetValue(MultiSelectProperty); set => SetValue(MultiSelectProperty, value); }

        /// <summary>Identifies the <see cref="SelectedIndex"/> property.</summary>
        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            nameof(SelectedIndex), typeof(int), typeof(SegmentedButton),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

        /// <summary>The lowest selected index, or -1. Setting it selects exactly that segment.</summary>
        public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

        /// <summary>Every selected index, ascending. Empty when nothing is on.</summary>
        public IReadOnlyList<int> SelectedIndices {
            get {
                var hits = new List<int>();
                int i = 0;
                foreach (SegmentedItem segment in Containers()) {
                    if (segment.IsChecked == true) hits.Add(i);
                    i++;
                }
                return hits;
            }
        }

        /// <summary>Whether the segment at <paramref name="index"/> is on. False for an index out of range.</summary>
        public bool IsSelected(int index) {
            SegmentedItem? item = Containers().ElementAtOrDefault(index);
            return item != null && item.IsChecked == true;
        }

        /// <summary>Turns one segment on or off, applying the single-select rule.</summary>
        public void SetSelected(int index, bool selected) {
            SegmentedItem? item = Containers().ElementAtOrDefault(index);
            if (item != null) item.IsChecked = selected;
        }

        // Re-entrancy guard: clearing the siblings and syncing SelectedIndex both raise the events this handler
        // listens to, so without it one click would recurse through the whole row.
        private bool _syncing;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new SegmentedItem();

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is SegmentedItem;

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);
            if (element is SegmentedItem segment) {
                segment.Checked += OnSegmentToggled;
                segment.Unchecked += OnSegmentToggled;
            }
            UpdatePositions();
            ApplyPendingSelection();
        }

        /// <inheritdoc />
        protected override void ClearContainerForItemOverride(DependencyObject element, object item) {
            if (element is SegmentedItem segment) {
                segment.Checked -= OnSegmentToggled;
                segment.Unchecked -= OnSegmentToggled;
            }
            base.ClearContainerForItemOverride(element, item);
            UpdatePositions();
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var row = (SegmentedButton)d;
            if (row._syncing) return;
            int wanted = (int)e.NewValue;
            row._syncing = true;
            try {
                int i = 0;
                foreach (SegmentedItem segment in row.Containers()) {
                    segment.IsChecked = i == wanted;
                    i++;
                }
            }
            finally { row._syncing = false; }
            row.SelectionChanged?.Invoke(row, EventArgs.Empty);
        }

        private void OnSegmentToggled(object sender, RoutedEventArgs e) {
            if (_syncing) return;
            var changed = (SegmentedItem)sender;
            _syncing = true;
            try {
                if (!MultiSelect && changed.IsChecked == true) {
                    foreach (SegmentedItem other in Containers()) {
                        if (!ReferenceEquals(other, changed)) other.IsChecked = false;
                    }
                }
                IReadOnlyList<int> selected = SelectedIndices;
                SelectedIndex = selected.Count > 0 ? selected[0] : -1;
            }
            finally { _syncing = false; }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // SelectedIndex is routinely set in the same initializer as ItemsSource, before a single container exists
        // — so the index is re-applied as the row fills, and only while nothing is selected yet.
        private void ApplyPendingSelection() {
            if (SelectedIndex < 0 || SelectedIndices.Count > 0) return;
            _syncing = true;
            try { SetSelected(SelectedIndex, true); }
            finally { _syncing = false; }
        }

        // Position, not an index, so the template can round the outer edges without a converter that would have
        // to be re-evaluated for every sibling whenever the row's length changes.
        private void UpdatePositions() {
            List<SegmentedItem> all = Containers().ToList();
            for (int i = 0; i < all.Count; i++) {
                SegmentPosition position =
                    all.Count == 1 ? SegmentPosition.Only :
                    i == 0 ? SegmentPosition.First :
                    i == all.Count - 1 ? SegmentPosition.Last : SegmentPosition.Middle;
                all[i].SetValue(SegmentedItem.PositionKey, position);
            }
        }

        private IEnumerable<SegmentedItem> Containers() {
            for (int i = 0; i < Items.Count; i++) {
                if (ItemContainerGenerator.ContainerFromIndex(i) is SegmentedItem segment) yield return segment;
            }
        }
    }
}
