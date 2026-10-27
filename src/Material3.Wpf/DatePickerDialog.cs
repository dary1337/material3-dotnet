using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>What a <see cref="DatePickerDialog"/> opens on and says.</summary>
    public sealed class DatePickerOptions {
        /// <summary>The date selected when the dialog opens; its month is the one shown.</summary>
        public DateTime Initial { get; set; } = DateTime.Today;

        /// <summary>The line above the selected date.</summary>
        public string Title { get; set; } = LibraryText.Get(TextKey.SelectDate);

        /// <summary>The confirm label.</summary>
        public string ConfirmText { get; set; } = LibraryText.Get(TextKey.Ok);

        /// <summary>The cancel label.</summary>
        public string CancelText { get; set; } = LibraryText.Get(TextKey.Cancel);

        /// <summary>The culture the month, the weekdays and the headline are written in, and the week starts by. Null is the current one.</summary>
        public CultureInfo? Culture { get; set; }
    }

    /// <summary>
    /// The M3 docked date picker as a blocking dialog: the selected date as the headline, month navigation, and a grid
    /// of days with today outlined. The arrow keys move the selection by a day or a week, Page Up and Page Down by a month.
    /// </summary>
    public static class DatePickerDialog {
        /// <summary>Shows the picker over <paramref name="owner"/>; returns the picked date, or null when it was cancelled.</summary>
        public static DateTime? Show(Window? owner, DatePickerOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            double cell = Resource(owner, "DatePickerCellSize", 40.0);
            var calendar = new DateGrid(options.Initial.Date, options.Culture ?? CultureInfo.CurrentCulture, options.Title, cell);
            DialogChoice choice = Dialog.Show(owner, new DialogOptions {
                Content = calendar,
                ConfirmText = options.ConfirmText,
                CancelText = options.CancelText,
                IsHeaderHidden = true,
                CardWidth = (cell + DateGrid.CellRoom) * 7,
            });
            return choice == DialogChoice.Confirm ? calendar.Selected : (DateTime?)null;
        }

        internal static T Resource<T>(Window? owner, string key, T fallback) =>
            owner?.TryFindResource(key) is T own ? own : Application.Current?.TryFindResource(key) is T app ? app : fallback;
    }

    internal sealed class DateGrid : StackPanel {
        // A day is a circle of the cell size centred in a cell this much wider and taller, as M3 lays the month out.
        internal const double CellRoom = 8;

        private readonly double _cell;

        private readonly CultureInfo _culture;
        private readonly DayOfWeek _firstDay;
        private readonly TextBlock _headline = new TextBlock { FontSize = 32, Margin = new Thickness(0, 4, 0, 0) };
        private readonly TextBlock _month = new TextBlock { FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        private readonly UniformGrid _days = new UniformGrid { Rows = 6, Columns = 7 };
        private DateTime _shown;

        internal DateGrid(DateTime initial, CultureInfo culture, string title, double cell = 40) {
            _cell = cell;
            _culture = culture;
            _firstDay = culture.DateTimeFormat.FirstDayOfWeek;
            Selected = initial;
            _shown = new DateTime(initial.Year, initial.Month, 1);
            Focusable = true;
            FocusVisualStyle = null;

            var overline = new TextBlock { Text = title, FontSize = 14 };
            overline.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            overline.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
            _month.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
            _headline.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
            Children.Add(overline);
            Children.Add(_headline);
            var divider = new Border { Height = 1, Margin = new Thickness(0, 12, 0, 8) };
            divider.SetResourceReference(Border.BackgroundProperty, "OutlineVariant");
            Children.Add(divider);

            var nav = new DockPanel { Height = 48 };
            Button next = NavButton("ChevronRight", 1);
            DockPanel.SetDock(next, Dock.Right);
            Button previous = NavButton("ChevronLeft", -1);
            DockPanel.SetDock(previous, Dock.Right);
            _month.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            _month.Margin = new Thickness(12, 0, 0, 0);
            nav.Children.Add(next);
            nav.Children.Add(previous);
            nav.Children.Add(_month);
            Children.Add(nav);

            var weekdays = new UniformGrid { Rows = 1, Columns = 7, Height = _cell + CellRoom };
            foreach (string letter in WeekdayLetters(_firstDay, culture)) {
                var head = new TextBlock { Text = letter, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                head.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
                weekdays.Children.Add(head);
            }
            Children.Add(weekdays);
            Children.Add(_days);

            Loaded += (_, __) => Focus();
            Render();
        }

        internal DateTime Selected { get; private set; }

        internal DateTime ShownMonth => _shown;

        internal static int FirstDayOffset(DateTime month, DayOfWeek firstDay) => ((int)month.DayOfWeek - (int)firstDay + 7) % 7;

        internal static string[] WeekdayLetters(DayOfWeek firstDay, CultureInfo culture) {
            string[] names = culture.DateTimeFormat.AbbreviatedDayNames;
            var letters = new string[7];
            for (int i = 0; i < 7; i++) {
                string name = names[((int)firstDay + i) % 7];
                letters[i] = name.Length > 0 ? name.Substring(0, 1).ToUpper(culture) : string.Empty;
            }
            return letters;
        }

        internal void Select(DateTime date) {
            Selected = date.Date;
            _shown = new DateTime(Selected.Year, Selected.Month, 1);
            Render();
        }

        internal void ShowMonth(int delta) {
            _shown = _shown.AddMonths(delta);
            Render();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            int days = e.Key switch { Key.Left => -1, Key.Right => 1, Key.Up => -7, Key.Down => 7, _ => 0 };
            if (days != 0) {
                Select(Selected.AddDays(days));
            }
            else if (e.Key == Key.PageUp) {
                Select(Selected.AddMonths(-1));
            }
            else if (e.Key == Key.PageDown) {
                Select(Selected.AddMonths(1));
            }
            else { base.OnKeyDown(e); return; }
            e.Handled = true;
        }

        private Button NavButton(string glyph, int delta) {
            var icon = new PathIcon { Glyph = glyph, Width = 20, Height = 20 };
            var button = new Button { Content = icon, Width = 40, Height = 40 };
            button.SetResourceReference(StyleProperty, "RowIconButton");
            button.Click += (_, __) => ShowMonth(delta);
            return button;
        }

        private void Render() {
            _headline.Text = Selected.ToString("ddd, MMM d", _culture);
            _month.Text = _shown.ToString("Y", _culture);
            _days.Children.Clear();
            int offset = FirstDayOffset(_shown, _firstDay);
            int length = DateTime.DaysInMonth(_shown.Year, _shown.Month);
            for (int cell = 0; cell < 42; cell++) {
                int day = cell - offset + 1;
                _days.Children.Add(day >= 1 && day <= length ? DayCell(new DateTime(_shown.Year, _shown.Month, day)) : new Border());
            }
        }

        private UIElement DayCell(DateTime date) {
            bool selected = date == Selected, today = date == DateTime.Today;
            var fill = new Ellipse { Width = _cell, Height = _cell };
            var state = new Ellipse { Width = _cell, Height = _cell, Opacity = 0 };
            var label = new TextBlock {
                Text = date.Day.ToString(_culture),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (selected) {
                fill.SetResourceReference(Shape.FillProperty, "Primary");
                label.SetResourceReference(TextBlock.ForegroundProperty, "OnPrimary");
                state.SetResourceReference(Shape.FillProperty, "OnPrimary");
            }
            else {
                if (today) {
                    fill.StrokeThickness = 1;
                    fill.SetResourceReference(Shape.StrokeProperty, "Primary");
                }
                label.SetResourceReference(TextBlock.ForegroundProperty, today ? "Primary" : "OnSurface");
                state.SetResourceReference(Shape.FillProperty, "OnSurface");
            }
            var root = new Grid { Width = _cell + CellRoom, Height = _cell + CellRoom, Background = Brushes.Transparent, Cursor = Cursors.Hand, Tag = date };
            root.Children.Add(fill);
            root.Children.Add(state);
            root.Children.Add(label);
            root.MouseEnter += (_, __) => state.Opacity = 0.08;
            root.MouseLeave += (_, __) => state.Opacity = 0;
            root.MouseLeftButtonUp += (_, e) => { e.Handled = true; Select(date); };
            return root;
        }
    }
}
