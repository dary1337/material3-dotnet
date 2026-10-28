using System.Windows.Forms;

namespace Material3.WinForms.Tokens {
    /// <summary>
    /// Layout, spacing, shape and component-size tokens — the single source of truth instead of
    /// magic numbers. Values are device-independent pixels at 96 DPI; pair with Form.AutoScaleMode
    /// for high-DPI.
    /// </summary>
    public static class Shape {
        /// <summary>Extra small corner, 4px: text fields, small badges.</summary>
        public const int ExtraSmall = 4;
        /// <summary>Small corner, 8px: chips, menus, snackbars.</summary>
        public const int Small = 8;
        /// <summary>Medium corner, 12px: cards.</summary>
        public const int Medium = 12;
        /// <summary>Large corner, 16px: navigation drawers, sheets.</summary>
        public const int Large = 16;
        /// <summary>Large increased corner, 20px.</summary>
        public const int LargeIncreased = 20;
        /// <summary>Extra large corner, 28px: dialogs.</summary>
        public const int ExtraLarge = 28;

        /// <summary>Pill / stadium shape — effective radius clamps to half the smaller side.</summary>
        public const int Full = 9999;
    }

    /// <summary>4-px spacing grid plus the paddings shared by the stock controls.</summary>
    public static class Spacing {
        /// <summary>4px on the 4px spacing grid.</summary>
        public const int Space1 = 4;
        /// <summary>8px on the 4px spacing grid.</summary>
        public const int Space2 = 8;
        /// <summary>12px on the 4px spacing grid.</summary>
        public const int Space3 = 12;
        /// <summary>16px on the 4px spacing grid.</summary>
        public const int Space4 = 16;
        /// <summary>20px on the 4px spacing grid.</summary>
        public const int Space5 = 20;
        /// <summary>24px on the 4px spacing grid.</summary>
        public const int Space6 = 24;
        /// <summary>32px on the 4px spacing grid.</summary>
        public const int Space8 = 32;

        /// <summary>The padding of a window's content area.</summary>
        public static readonly Padding WindowBody = new Padding(20, 16, 20, 20);
        /// <summary>The padding inside a card.</summary>
        public static readonly Padding Card = new Padding(16);
        /// <summary>The padding of a list row.</summary>
        public static readonly Padding ListItem = new Padding(16, 12, 16, 12);
        /// <summary>The padding inside a dialog.</summary>
        public static readonly Padding Dialog = new Padding(24);
    }

    /// <summary>Default component dimensions (px @96 DPI).</summary>
    public static class ComponentSizes {
        /// <summary>The height of every standard control — buttons, icon buttons, selects, segmented buttons, the search bar, menu rows and selection rows. In 96-DPI pixels.</summary>
        public const int ControlHeight = 40;
        /// <summary>The height of an M3 text field with a floating label. In 96-DPI pixels.</summary>
        public const int FieldHeight = 56;
        /// <summary>The height of compact buttons and dense rows. In 96-DPI pixels.</summary>
        public const int CompactControlHeight = 32;
        /// <summary>A text button's height. In 96-DPI pixels.</summary>
        public const int TextButtonHeight = 36;
        /// <summary>A status chip's height. In 96-DPI pixels.</summary>
        public const int ChipHeight = 24;
        /// <summary>An interactive chip's height. In 96-DPI pixels.</summary>
        public const int AssistChipHeight = 32;
        /// <summary>The least height of a one-line list row. In 96-DPI pixels.</summary>
        public const int ListItemMinHeight = 56;
        /// <summary>The thickness of a linear progress bar. In 96-DPI pixels.</summary>
        public const int LinearProgressHeight = 4;
        /// <summary>The widest a dialog grows. In 96-DPI pixels.</summary>
        public const int DialogMaxWidth = 420;
        /// <summary>A title bar's height. In 96-DPI pixels.</summary>
        public const int TitleBarHeight = 40;

        /// <summary>The smallest glyph size. In 96-DPI pixels.</summary>
        public const int IconExtraSmall = 16;
        /// <summary>A small glyph size. In 96-DPI pixels.</summary>
        public const int IconSmall = 18;
        /// <summary>The glyph size inside buttons and fields. In 96-DPI pixels.</summary>
        public const int IconMedium = 20;
        /// <summary>The standard 24px glyph size. In 96-DPI pixels.</summary>
        public const int IconLarge = 24;
    }

    /// <summary>
    /// M3 state-layer opacities: a translucent layer of the content color composited over the
    /// container while the state is active. Use with <see cref="Theming.ArgbExtensions.Overlay"/>.
    /// </summary>
    public static class StateLayers {
        /// <summary>State-layer opacity under the pointer.</summary>
        public const double Hover = 0.08;
        /// <summary>State-layer opacity on keyboard focus.</summary>
        public const double Focus = 0.12;
        /// <summary>State-layer opacity while pressed.</summary>
        public const double Pressed = 0.12;
        /// <summary>State-layer opacity while dragged.</summary>
        public const double Dragged = 0.16;

        /// <summary>Hover layer over a filled button that inverts a tone's container pair: its light content needs more to show.</summary>
        public const double InvertedHover = 0.10;
        /// <summary>Pressed layer over a filled button that inverts a tone's container pair.</summary>
        public const double InvertedPressed = 0.16;

        /// <summary>Container opacity for disabled fills (12% of OnSurface over the parent).</summary>
        public const double DisabledContainer = 0.12;

        /// <summary>Content opacity for disabled text/icons (38% of OnSurface).</summary>
        public const double DisabledContent = 0.38;
    }
}
