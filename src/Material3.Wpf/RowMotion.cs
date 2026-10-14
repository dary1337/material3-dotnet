using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    internal static class RowMotion {
        private static bool CanAnimate(ItemsControl? items) => items != null && items.IsVisible && Motion.IsAnimating;

        private static FrameworkElement? Container(ItemsControl items, object item) =>
            items.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

        internal static void Added(ItemsControl? items, object item) {
            if (!CanAnimate(items)) return;
            items!.UpdateLayout();
            if (Container(items, item) is FrameworkElement row) Motion.GrowIn(row);
        }

        internal static void Remove(ItemsControl? items, object item, HashSet<object> leaving, Action remove) {
            if (leaving.Contains(item)) return;
            if (!CanAnimate(items) || !(Container(items!, item) is FrameworkElement row)) {
                remove();
                return;
            }
            leaving.Add(item);
            Motion.ShrinkOut(row, () => {
                leaving.Remove(item);
                remove();
            });
        }

        internal static void Move(ItemsControl? items, IEnumerable<object> affected, Action move) {
            if (!CanAnimate(items)) {
                move();
                return;
            }
            var before = new List<(object Item, Point At)>();
            foreach (object item in affected)
                if (Container(items!, item) is FrameworkElement row) before.Add((item, row.TranslatePoint(new Point(), items)));
            move();
            items!.UpdateLayout();
            foreach ((object item, Point at) in before)
                if (Container(items, item) is FrameworkElement row) Motion.SlideFrom(row, at - row.TranslatePoint(new Point(), items));
        }
    }
}
