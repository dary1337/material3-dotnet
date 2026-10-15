using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Material3.Wpf {
    /// <summary>What a filtered <see cref="TextBox"/> lets the user type.</summary>
    public enum InputKind {
        /// <summary>Anything.</summary>
        Text,
        /// <summary>A signed decimal with a <c>.</c> separator, whatever the culture.</summary>
        Number,
        /// <summary>A signed whole number.</summary>
        Integer,
    }

    /// <summary>
    /// Refuses a keystroke or a paste that would leave a <see cref="TextBox"/> holding something its
    /// <see cref="InputKind"/> cannot become. Attach with <c>m3:InputFilter.Kind="Number"</c>.
    /// </summary>
    /// <remarks>
    /// It judges the text as it would read after the edit, not the typed character alone, so a minus sign is
    /// taken only at the start and a second separator is refused. An empty or half-typed value (<c>-</c>, <c>1.</c>)
    /// passes: the field is mid-edit, and only the caller knows whether it may stay that way.
    /// </remarks>
    public static class InputFilter {
        private static readonly Regex PartialNumber = new Regex(@"^-?[0-9]*\.?[0-9]*$");
        private static readonly Regex PartialInteger = new Regex(@"^-?[0-9]*$");

        /// <summary>Identifies the <c>Kind</c> attached property.</summary>
        public static readonly DependencyProperty KindProperty = DependencyProperty.RegisterAttached(
            "Kind", typeof(InputKind), typeof(InputFilter), new PropertyMetadata(InputKind.Text, OnKindChanged));

        /// <summary>Sets what <paramref name="element"/> accepts.</summary>
        public static void SetKind(DependencyObject element, InputKind value) => element.SetValue(KindProperty, value);

        /// <summary>Gets what <paramref name="element"/> accepts.</summary>
        public static InputKind GetKind(DependencyObject element) => (InputKind)element.GetValue(KindProperty);

        /// <summary>Whether <paramref name="text"/> is a value of <paramref name="kind"/>, or the start of one.</summary>
        public static bool Accepts(InputKind kind, string text) {
            switch (kind) {
                case InputKind.Number: return PartialNumber.IsMatch(text);
                case InputKind.Integer: return PartialInteger.IsMatch(text);
                default: return true;
            }
        }

        private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is TextBox box)) return;
            box.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(box, OnPasting);
            if ((InputKind)e.NewValue == InputKind.Text) return;
            box.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(box, OnPasting);
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e) {
            var box = (TextBox)sender;
            e.Handled = !Accepts(GetKind(box), Proposed(box, e.Text));
        }

        private static void OnPasting(object sender, DataObjectPastingEventArgs e) {
            var box = (TextBox)sender;
            string pasted = e.DataObject.GetDataPresent(DataFormats.UnicodeText)
                ? e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty
                : string.Empty;
            if (!Accepts(GetKind(box), Proposed(box, pasted))) e.CancelCommand();
        }

        internal static string Proposed(TextBox box, string insert) =>
            box.Text.Remove(box.SelectionStart, box.SelectionLength).Insert(box.SelectionStart, insert);
    }
}
