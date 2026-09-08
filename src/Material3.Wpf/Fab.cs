using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>The three M3 FAB sizes. The extended variant is any size with a label — set <c>Content</c>.</summary>
    public enum FabSize {
        /// <summary>40 px.</summary>
        Small,
        /// <summary>56 px — the default.</summary>
        Standard,
        /// <summary>96 px.</summary>
        Large,
    }

    /// <summary>
    /// The floating action button: the one action a screen exists for, carried on a level-3 surface so it reads
    /// as sitting above the content rather than in it. Give it <see cref="IconKind"/> alone for the round
    /// variant, or add <c>Content</c> for the extended pill.
    /// </summary>
    public class Fab : Button {
        static Fab() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Fab), new FrameworkPropertyMetadata(typeof(Fab)));
        }

        /// <summary>Identifies the <see cref="IconKind"/> property.</summary>
        public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
            nameof(IconKind), typeof(string), typeof(Fab), new PropertyMetadata(null));

        /// <summary>The glyph, resolved through the icon set registered with <see cref="M3Icon"/>.</summary>
        public string? IconKind { get => (string?)GetValue(IconKindProperty); set => SetValue(IconKindProperty, value); }

        /// <summary>Identifies the <see cref="Size"/> property.</summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size), typeof(FabSize), typeof(Fab), new PropertyMetadata(FabSize.Standard));

        /// <summary>Which of the three M3 sizes to draw.</summary>
        public FabSize Size { get => (FabSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    }
}
