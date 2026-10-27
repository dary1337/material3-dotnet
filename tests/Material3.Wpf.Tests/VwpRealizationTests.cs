using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;
using Xunit.Abstractions;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class VwpRealizationTests {
        private readonly ITestOutputHelper _o;
        public VwpRealizationTests(ITestOutputHelper o) => _o = o;

        [Fact]
        public void RealizesOnlyAround_ViewportWorth_Not1000() {
            int realized = -1;
            Exception? err = null;
            var t = new Thread(() => {
                try { realized = RunLayout(); }
                catch (Exception e) { err = e; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (err != null) {
                throw new Exception("layout threw: " + err, err);
            }

            _o.WriteLine("realized containers = " + realized);
            Assert.True(realized > 0, "nothing realized");
            Assert.True(realized < 200, $"VWP did not virtualize: {realized} of 1000 realized");
        }

        [Fact]
        public void RemovingItems_DoesNotThrow() {
            Exception? err = null;
            var t = new Thread(() => { try { RunRemoval(); } catch (Exception e) { err = e; } });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (err != null) {
                throw new Exception("remove threw: " + err, err);
            }
        }

        private static void RunRemoval() {
            var items = new System.Collections.ObjectModel.ObservableCollection<int>(Enumerable.Range(0, 200));
            var lb = BuildListBox(items);
            var win = new Window {
                Width = 820,
                Height = 440,
                Left = -5000,
                Top = -5000,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Content = lb,
            };
            win.Show();
            win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            win.UpdateLayout();
            lb.ScrollIntoView(items[items.Count - 1]);
            win.UpdateLayout();
            lb.ScrollIntoView(items[0]);
            win.UpdateLayout();
            items.RemoveAt(0);
            win.UpdateLayout();
            items.RemoveAt(3);
            win.UpdateLayout();
            lb.ScrollIntoView(items[items.Count - 1]);
            win.UpdateLayout();
            items.RemoveAt(items.Count - 1);
            win.UpdateLayout();
            items.Clear();
            win.UpdateLayout();
            win.UpdateLayout();
            win.Close();
        }

        private static ListBox BuildListBox(System.Collections.IEnumerable source) {
            var lb = new ListBox { Width = 800, Height = 400, ItemsSource = source };
            lb.SetValue(ScrollViewer.CanContentScrollProperty, true);
            lb.SetValue(VirtualizingPanel.IsVirtualizingProperty, true);
            lb.SetValue(VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
            lb.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingWrapPanel)));
            var cardFactory = new FrameworkElementFactory(typeof(Border));
            cardFactory.SetValue(FrameworkElement.WidthProperty, 160.0);
            cardFactory.SetValue(FrameworkElement.HeightProperty, 150.0);
            lb.ItemTemplate = new DataTemplate { VisualTree = cardFactory };
            return lb;
        }

        private static int RunLayout() {
            var lb = BuildListBox(Enumerable.Range(0, 1000).ToList());

            var win = new Window {
                Width = 820,
                Height = 440,
                Left = -5000,
                Top = -5000,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Content = lb,
            };
            win.Show();
            win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            win.UpdateLayout();
            win.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            int n = CountContainers(lb);
            win.Close();
            return n;
        }

        private static int CountContainers(DependencyObject root) {
            int n = 0;
            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is ListBoxItem || child is Border) {
                    n++;
                }

                n += CountContainers(child);
            }
            return n;
        }
    }
}
