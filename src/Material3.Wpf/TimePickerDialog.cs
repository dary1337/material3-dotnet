using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>What a <see cref="TimePickerDialog"/> opens on and says.</summary>
    public sealed class TimePickerOptions {
        /// <summary>The time the fields open with.</summary>
        public TimeSpan Initial { get; set; } = DateTime.Now.TimeOfDay;

        /// <summary>A 24-hour clock instead of AM/PM. Null follows the culture's short time pattern.</summary>
        public bool? Is24Hour { get; set; }

        /// <summary>The line above the fields.</summary>
        public string Title { get; set; } = LibraryText.Get(TextKey.EnterTime);

        /// <summary>The caption under the hour field.</summary>
        public string HourText { get; set; } = LibraryText.Get(TextKey.Hour);

        /// <summary>The caption under the minute field.</summary>
        public string MinuteText { get; set; } = LibraryText.Get(TextKey.Minute);

        /// <summary>The confirm label.</summary>
        public string ConfirmText { get; set; } = LibraryText.Get(TextKey.Ok);

        /// <summary>The cancel label.</summary>
        public string CancelText { get; set; } = LibraryText.Get(TextKey.Cancel);

        /// <summary>The culture the AM/PM designators come from. Null is the current one.</summary>
        public CultureInfo? Culture { get; set; }
    }

    /// <summary>
    /// The M3 time input as a blocking dialog: an hour and a minute field and, on a 12-hour clock, an AM/PM toggle.
    /// A typed value out of range is clamped, so the dialog always answers with a real time of day.
    /// </summary>
    public static class TimePickerDialog {
        /// <summary>Shows the picker over <paramref name="owner"/>; returns the time, or null when it was cancelled.</summary>
        public static TimeSpan? Show(Window? owner, TimePickerOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            CultureInfo culture = options.Culture ?? CultureInfo.CurrentCulture;
            bool is24 = options.Is24Hour ?? !culture.DateTimeFormat.ShortTimePattern.Contains("h");
            var input = new TimeInput(options, culture, is24,
                DatePickerDialog.Resource(owner, "TimePickerFieldSize", new Size(96, 72)),
                DatePickerDialog.Resource(owner, "TimePickerFieldFontSize", 45.0));
            DialogChoice choice = Dialog.Show(owner, new DialogOptions {
                Content = input,
                ConfirmText = options.ConfirmText,
                CancelText = options.CancelText,
                IsHeaderHidden = true,
                CardWidth = 0,
            });
            return choice == DialogChoice.Confirm ? input.Value : (TimeSpan?)null;
        }
    }

    internal sealed class TimeInput : StackPanel {
        private readonly bool _is24;
        private readonly TextBox _hour;
        private readonly TextBox _minute;
        private readonly bool _hasPeriod;
        private bool _pm;
        private Border? _am, _pmRow;

        internal TimeInput(TimePickerOptions options, CultureInfo culture, bool is24, Size field = default, double fontSize = 45) {
            _is24 = is24;
            if (field.Width <= 0) {
                field = new Size(96, 72);
            }

            var overline = new TextBlock { Text = options.Title, FontSize = 12, Margin = new Thickness(0, 0, 0, 20) };
            overline.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            overline.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
            Children.Add(overline);

            int hour = options.Initial.Hours, minute = options.Initial.Minutes;
            bool pm = hour >= 12;
            int shownHour = is24 ? hour : (hour % 12 == 0 ? 12 : hour % 12);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            _hour = Field(shownHour, options.HourText, field, fontSize, out FrameworkElement hourColumn);
            _minute = Field(minute, options.MinuteText, field, fontSize, out FrameworkElement minuteColumn);
            var colon = new TextBlock {
                Text = ":",
                FontSize = fontSize * 57 / 45,
                Width = field.Width / 4,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, -6, 0, 0),
            };
            colon.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
            row.Children.Add(hourColumn);
            row.Children.Add(colon);
            row.Children.Add(minuteColumn);
            if (!is24) {
                _hasPeriod = true;
                row.Children.Add(PeriodSelector(culture, field.Height));
                ChooseAfternoon(pm);
            }
            Children.Add(row);
            Loaded += (_, __) => _hour.Focus();
        }

        internal TimeSpan Value => Normalize(Read(_hour), Read(_minute), _hasPeriod && _pm, _is24);

        // The M3 period selector: AM over PM in one outlined 52-wide column beside the fields, the chosen half tertiary.
        private FrameworkElement PeriodSelector(CultureInfo culture, double height) {
            Border Half(string text, CornerRadius corners, bool afternoon) {
                var label = new TextBlock { Text = text, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                label.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
                var half = new Border { CornerRadius = corners, Background = Brushes.Transparent, Cursor = Cursors.Hand, Child = label };
                half.MouseLeftButtonUp += (_, e) => { e.Handled = true; ChooseAfternoon(afternoon); };
                return half;
            }
            _am = Half(culture.DateTimeFormat.AMDesignator, new CornerRadius(7, 7, 0, 0), false);
            _pmRow = Half(culture.DateTimeFormat.PMDesignator, new CornerRadius(0, 0, 7, 7), true);
            var divider = new Border { Height = 1 };
            divider.SetResourceReference(Border.BackgroundProperty, "Outline");
            var halves = new Grid();
            halves.RowDefinitions.Add(new RowDefinition());
            halves.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            halves.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(divider, 1);
            Grid.SetRow(_pmRow, 2);
            halves.Children.Add(_am);
            halves.Children.Add(divider);
            halves.Children.Add(_pmRow);
            var column = new Border {
                Name = "period",
                Width = 52,
                Height = height,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Child = halves,
            };
            column.SetResourceReference(Border.BorderBrushProperty, "Outline");
            return column;
        }

        internal static TimeSpan Normalize(int hour, int minute, bool isPm, bool is24) {
            hour = Math.Max(is24 ? 0 : 1, Math.Min(is24 ? 23 : 12, hour));
            minute = Math.Max(0, Math.Min(59, minute));
            return new TimeSpan(is24 ? hour : hour % 12 + (isPm ? 12 : 0), minute, 0);
        }

        internal void Type(int hour, int minute) {
            _hour.Text = hour.ToString(CultureInfo.InvariantCulture);
            _minute.Text = minute.ToString(CultureInfo.InvariantCulture);
        }

        internal void ChooseAfternoon(bool pm) {
            if (!_hasPeriod || _am == null || _pmRow == null) {
                return;
            }

            _pm = pm;
            foreach ((Border half, bool chosen) in new[] { (_am, !pm), (_pmRow, pm) }) {
                if (chosen) {
                    half.SetResourceReference(Border.BackgroundProperty, "TertiaryContainer");
                }
                else {
                    half.Background = Brushes.Transparent;
                }

                ((TextBlock)half.Child).SetResourceReference(TextBlock.ForegroundProperty, chosen ? "OnTertiaryContainer" : "OnSurfaceVariant");
            }
        }

        private static int Read(TextBox field) =>
            int.TryParse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;

        private static TextBox Field(int value, string caption, Size size, double fontSize, out FrameworkElement column) {
            var box = new TextBox {
                Text = value.ToString("00", CultureInfo.InvariantCulture),
                Width = size.Width,
                Height = size.Height,
                FontSize = fontSize,
                MaxLength = 2,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
            };
            InputFilter.SetKind(box, InputKind.Integer);
            box.SetResourceReference(Control.ForegroundProperty, "OnSurface");
            box.SetResourceReference(Control.BackgroundProperty, "SurfaceContainerHighest");
            bool overwrite = false;
            box.GotKeyboardFocus += (_, __) => {
                box.SetResourceReference(Control.BorderBrushProperty, "Primary");
                box.SetResourceReference(Control.BackgroundProperty, "PrimaryContainer");
                box.SetResourceReference(Control.ForegroundProperty, "OnPrimaryContainer");
                box.CaretIndex = box.Text.Length;
                overwrite = true;
            };
            box.LostKeyboardFocus += (_, __) => {
                box.BorderBrush = Brushes.Transparent;
                box.SetResourceReference(Control.BackgroundProperty, "SurfaceContainerHighest");
                box.SetResourceReference(Control.ForegroundProperty, "OnSurface");
            };
            box.PreviewTextInput += (_, __) => {
                if (!overwrite) {
                    return;
                }

                overwrite = false;
                box.Clear();
            };
            box.PreviewKeyDown += (_, args) => {
                if (args.Key == Key.Back || args.Key == Key.Delete || args.Key == Key.Left || args.Key == Key.Right) {
                    overwrite = false;
                }
            };
            var label = new TextBlock { Text = caption, FontSize = 12, Margin = new Thickness(0, 7, 0, 0) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            var stack = new StackPanel();
            stack.Children.Add(box);
            stack.Children.Add(label);
            column = stack;
            return box;
        }
    }
}
