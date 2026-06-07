using System.Windows.Forms;

namespace Material3.WinForms.Tokens {
    /// <summary>
    /// Layout, spacing, shape and component-size tokens — the single source of truth instead of
    /// magic numbers. Values are device-independent pixels at 96 DPI; pair with Form.AutoScaleMode
    /// for high-DPI.
    /// </summary>
    public static class Shape {
        // M3 shape scale · corner radius
        public const int ExtraSmall = 4;
        public const int Small = 8;
        public const int Medium = 12;
        public const int Large = 16;
        public const int LargeIncreased = 20;
        public const int ExtraLarge = 28;

        /// <summary>Pill / stadium shape — effective radius clamps to half the smaller side.</summary>
        public const int Full = 9999;
    }

    /// <summary>4-px spacing grid plus the paddings shared by the stock controls.</summary>
    public static class Spacing {
        public const int Space1 = 4;
        public const int Space2 = 8;
        public const int Space3 = 12;
        public const int Space4 = 16;
        public const int Space5 = 20;
        public const int Space6 = 24;
        public const int Space8 = 32;

        public static readonly Padding WindowBody = new Padding(20, 16, 20, 20);
        public static readonly Padding Card = new Padding(16);
        public static readonly Padding ListItem = new Padding(16, 12, 16, 12);
        public static readonly Padding Dialog = new Padding(24);
    }

    /// <summary>Default component dimensions (px @96 DPI).</summary>
    public static class ComponentSizes {
        public const int ButtonHeight = 40;
        public const int ButtonHeightSmall = 32;
        public const int TextButtonHeight = 36;
        public const int ChipHeight = 24;
        public const int AssistChipHeight = 32;
        public const int ListItemMinHeight = 56;
        public const int DropdownHeight = 48;
        public const int LinearProgressHeight = 8;
        public const int DialogMaxWidth = 420;
        public const int TitleBarHeight = 40;

        public const int IconExtraSmall = 16;
        public const int IconSmall = 18;
        public const int IconMedium = 20;
        public const int IconLarge = 24;
    }

    /// <summary>
    /// M3 state-layer opacities: a translucent layer of the content color composited over the
    /// container while the state is active. Use with <see cref="Theming.ColorScheme.Overlay"/>.
    /// </summary>
    public static class StateLayers {
        public const double Hover = 0.08;
        public const double Focus = 0.12;
        public const double Pressed = 0.12;
        public const double Dragged = 0.16;

        /// <summary>Container opacity for disabled fills (12% of OnSurface over the parent).</summary>
        public const double DisabledContainer = 0.12;

        /// <summary>Content opacity for disabled text/icons (38% of OnSurface).</summary>
        public const double DisabledContent = 0.38;
    }
}
