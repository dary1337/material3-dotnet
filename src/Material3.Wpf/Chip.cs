using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    public enum ChipSeverity { Neutral, Primary, Warning, Error }

    // Compact status chip (icon + label, tinted by Severity). Styled by the implicit template in Controls.xaml.
    public sealed class Chip : Control {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(Chip), new PropertyMetadata(string.Empty));
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(nameof(IconKind), typeof(string), typeof(Chip), new PropertyMetadata("None"));
        public string IconKind { get => (string)GetValue(IconKindProperty); set => SetValue(IconKindProperty, value); }

        public static readonly DependencyProperty SeverityProperty =
            DependencyProperty.Register(nameof(Severity), typeof(ChipSeverity), typeof(Chip), new PropertyMetadata(ChipSeverity.Neutral));
        public ChipSeverity Severity { get => (ChipSeverity)GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
    }
}
