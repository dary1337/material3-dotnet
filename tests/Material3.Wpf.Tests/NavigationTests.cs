using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace Material3.Wpf.Tests {
    public class BuiltInGlyphTests {
        [Fact]
        public void EveryBuiltInGlyphDrawsInsideTheBox() {
            foreach (string key in PathIcon.BuiltInKeys) {
                Geometry? geometry = BuiltInGlyphs.Resolve(key);
                Assert.NotNull(geometry);
                Rect bounds = geometry!.Bounds;
                Assert.False(bounds.IsEmpty, key);
                Assert.InRange(bounds.Left, -0.01, BuiltInGlyphs.Box);
                Assert.InRange(bounds.Top, -0.01, BuiltInGlyphs.Box);
                Assert.InRange(bounds.Right, 0, BuiltInGlyphs.Box + 0.01);
                Assert.InRange(bounds.Bottom, 0, BuiltInGlyphs.Box + 0.01);
            }
        }

        [Fact]
        public void TheGlyphsTheTemplatesUseAreBuiltIn() {
            foreach (string key in new[] { "Check", "Close", "ChevronDown", "ChevronUp", "ChevronRight", "Loading", "Plus", "Magnify", "OpenInNew",
                         "Information", "InformationOutline", "AlertOutline", "AlertCircle", "CheckCircle" }) {
                Assert.Contains(key, PathIcon.BuiltInKeys);
            }
        }

        [Fact]
        public void ARotatedChevronPointsWhereItsNameSays() {
            Rect right = BuiltInGlyphs.Resolve("ChevronRight")!.Bounds;
            Rect down = BuiltInGlyphs.Resolve("ChevronDown")!.Bounds;
            Assert.True(right.Height > right.Width);
            Assert.True(down.Width > down.Height);
        }
    }

    [Trait("Category", "Ui")]
    public class NavigationTests {
        private static NavigationRail Rail(Window w, params NavigationItem[] items) {
            var rail = new NavigationRail();
            foreach (NavigationItem item in items) {
                rail.Items.Add(item);
            }

            w.Content = rail;
            Ui.Settle(w);
            return rail;
        }

        [Fact]
        public void TheFirstDestinationStartsSelected() => Ui.InWindow(w => {
            NavigationRail rail = Rail(w, new NavigationItem { Content = "Home", Glyph = "Check" }, new NavigationItem { Content = "Mail", Glyph = "Close" });
            Assert.Equal(0, rail.SelectedIndex);
        });

        [Fact]
        public void TheSelectedDestinationCannotBeDeselected() => Ui.InWindow(w => {
            var home = new NavigationItem { Content = "Home" };
            NavigationRail rail = Rail(w, home, new NavigationItem { Content = "Mail" });
            int changes = 0;
            rail.SelectionChanged += (_, __) => changes++;

            home.IsSelected = false;

            Assert.Same(home, rail.SelectedItem);
            Assert.True(home.IsSelected);
            Assert.Equal(0, changes);
        });

        [Fact]
        public void SelectingAnotherDestinationMovesTheSelection() => Ui.InWindow(w => {
            var home = new NavigationItem { Content = "Home" };
            var mail = new NavigationItem { Content = "Mail" };
            NavigationRail rail = Rail(w, home, mail);
            SelectionChangedEventArgs? seen = null;
            rail.SelectionChanged += (_, e) => seen = e;

            mail.IsSelected = true;

            Assert.Same(mail, rail.SelectedItem);
            Assert.False(home.IsSelected);
            Assert.Same(mail, seen!.AddedItems.Cast<object>().Single());
            Assert.Same(home, seen.RemovedItems.Cast<object>().Single());
        });

        [Fact]
        public void TheSelectedLabelKeepsTheIdleWeight() => Ui.InWindow(w => {
            var home = new NavigationItem { Content = "Home" };
            var mail = new NavigationItem { Content = "Mail" };
            Rail(w, home, mail);
            TextBlock Label(NavigationItem item) => Ui.Descendants<TextBlock>(item).First(t => t.Name == "label");
            Assert.Equal(Label(mail).FontWeight, Label(home).FontWeight);
        });

        [Fact]
        public void TheSelectedGlyphReplacesTheIdleOneOnlyWhileSelected() => Ui.InWindow(w => {
            var home = new NavigationItem { Content = "Home", Glyph = "InformationOutline", SelectedGlyph = "Information" };
            var mail = new NavigationItem { Content = "Mail", Glyph = "Close" };
            Rail(w, home, mail);
            PathIcon Icon(NavigationItem item) => Ui.Descendants<PathIcon>(item).First(i => i.Name != "busy");

            Assert.Equal("Information", Icon(home).Glyph);
            mail.IsSelected = true;
            Ui.Settle(w);
            Assert.Equal("InformationOutline", Icon(home).Glyph);
            Assert.Equal("Close", Icon(mail).Glyph);
        });

        [Fact]
        public void BusyHidesTheBadgeAndSpins() => Ui.InWindow(w => {
            var item = new NavigationItem { Content = "Build", BadgeCount = 3 };
            Rail(w, item);
            Badge badge = Ui.Descendants<Badge>(item).Single();
            PathIcon busy = Ui.Descendants<PathIcon>(item).Single(i => i.Name == "busy");
            Assert.True(badge.IsVisible);
            Assert.Equal("3", badge.Text);
            Assert.False(busy.IsVisible);

            item.IsBusy = true;
            Ui.Settle(w);

            Assert.False(badge.IsVisible);
            Assert.True(busy.IsVisible);
        });

        [Fact]
        public void TheBadgeTakesTheItemsTone() => Ui.InWindow(w => {
            var item = new NavigationItem { Content = "Decks", IsBadgeDot = true, BadgeTone = Tone.Accent };
            Rail(w, item);
            Badge badge = Ui.Descendants<Badge>(item).Single();
            Assert.True(badge.IsDot);
            Assert.Equal(Tone.Accent, badge.Tone);
            Assert.Same(w.FindResource("Primary"), Ui.Descendants<System.Windows.Shapes.Ellipse>(badge).Single().Fill);
        });

        [Fact]
        public void TheBarAndTheDrawerDressTheirItems() => Ui.InWindow(w => {
            var bar = new NavigationBar();
            var barItem = new NavigationItem { Content = "Home" };
            bar.Items.Add(barItem);
            var drawer = new NavigationDrawer();
            var drawerItem = new NavigationItem { Content = "Inbox" };
            drawer.Items.Add(drawerItem);
            var root = new StackPanel();
            root.Children.Add(bar);
            root.Children.Add(drawer);
            w.Content = root;
            Ui.Settle(w);

            Assert.Same(w.FindResource("NavigationBarItem"), barItem.Style);
            Assert.Same(w.FindResource("NavigationDrawerItem"), drawerItem.Style);
            TextBlock headline = Ui.Descendants<TextBlock>(drawer).Single(t => t.Name == "headline");
            Assert.False(headline.IsVisible);
            drawer.Headline = "Mail";
            Ui.Settle(w);
            Assert.True(headline.IsVisible);
        });

        [Fact]
        public void ABoundListGetsNavigationItems() => Ui.InWindow(w => {
            var rail = new NavigationRail { ItemsSource = new[] { "Home", "Mail" } };
            w.Content = rail;
            Ui.Settle(w);
            Assert.IsType<NavigationItem>(rail.ItemContainerGenerator.ContainerFromIndex(1));
            Assert.Equal(0, rail.SelectedIndex);
        });
    }
}
