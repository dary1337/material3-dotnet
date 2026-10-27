using System;
using System.Linq;

namespace Material3.Gallery {
    // One list of pages and sections both galleries render, so their order and wording cannot drift apart.
    internal sealed class GallerySection {
        public GallerySection(string id, string title, string caption = "", string? only = null) {
            Id = id;
            Title = title;
            Caption = caption;
            Only = only;
        }

        public string Id { get; }
        public string Title { get; }
        public string Caption { get; }

        // "WPF" or "WinForms" for a section that shows what only that stack has; null on both.
        public string? Only { get; }

        public bool ShowsOn(string stack) => Only == null || Only == stack;
    }

    internal sealed class GalleryPage {
        public GalleryPage(string title, string caption, params GallerySection[] sections) {
            Title = title;
            Caption = caption;
            Sections = sections;
        }

        public string Title { get; }
        public string Caption { get; }
        public GallerySection[] Sections { get; }
    }

    internal static class GallerySpec {
        public const string Wpf = "WPF";
        public const string WinForms = "WinForms";
        public const string TypeSample = "The quick brown fox";

        public static readonly GalleryPage[] Pages = {
            new GalleryPage("Color roles", "Every role of the active scheme. Switch the seed, variant or mode on the left and every control follows.",
                new GallerySection("colors.primary", "Primary"),
                new GallerySection("colors.secondary", "Secondary / Tertiary"),
                new GallerySection("colors.semantic", "Semantic"),
                new GallerySection("colors.surfaces", "Surfaces"),
                new GallerySection("colors.content", "Content & outline")),
            new GalleryPage("Typography", "",
                new GallerySection("type.scale", "Type scale", "Every M3 type role the controls draw their text with.")),
            new GalleryPage("Elevation & Shape", "",
                new GallerySection("shape.elevation", "Elevation",
                    "Levels 0–5: a shadow and a surface tint that grows with the level. On a dark scheme the shadow barely shows, so the tint is what carries the height."),
                new GallerySection("shape.scale", "Shape scale", "The corner-radius scale every control draws from.")),
            new GalleryPage("Buttons & FAB", "",
                new GallerySection("buttons.variants", "Buttons", "Elevated · Filled · Tonal · Outlined · Text. Hover and press to see the state layers."),
                new GallerySection("buttons.semantic", "Semantic",
                    "Warning and error fills read as THE action of a banner in that colour; the tonal error is a destructive choice in a dialog; the shiny one is the single action a screen waits on."),
                new GallerySection("buttons.icon", "With icon", "A leading glyph follows the label's colour, the disabled one included."),
                new GallerySection("buttons.disabled", "Disabled"),
                new GallerySection("buttons.iconbuttons", "Icon buttons",
                    "Standard · Filled · Tonal · Outlined, and the error button whose hover warns that the click removes something."),
                new GallerySection("buttons.icontoggles", "Icon toggles", "The same four as toggles: each has one colour pair for off and one for on."),
                new GallerySection("buttons.fab", "FAB", "The one action a screen exists for. Small · Standard · Large, and extended with a label.")),
            new GalleryPage("Text inputs", "",
                new GallerySection("inputs.fields", "Text fields",
                    "Filled and outlined, with a label that rests in the empty field and floats above the text once the field has focus or content."),
                new GallerySection("inputs.plain", "Plain text box", "The stock TextBox in the M3 look, with a placeholder hint.", Wpf),
                new GallerySection("inputs.search", "Search bar", "The clear button appears once there is something to clear. Enter submits, and Esc clears first."),
                new GallerySection("inputs.number", "Number filter",
                    "Refuses a keystroke or a paste that would leave something that cannot become a number. A half-typed value such as - or 1. still passes."),
                new GallerySection("inputs.color", "Colour field",
                    "The swatch opens a saturation/value plane with hue and alpha rails; the hex beside it takes an exact value on Enter. A colour that follows another one says so instead, and refuses the edit."),
                new GallerySection("inputs.list", "Value list",
                    "A list is never a text box holding commas: one missing comma changes every entry after it. Each entry is its own field, with move and remove beside it."),
                new GallerySection("inputs.slots", "Fixed slots", "A fixed count of labelled slots that cannot be added or moved — a handful of numbers that reads as one value on one line."),
                new GallerySection("inputs.table", "Record table",
                    "Records of one shape, with the columns given as data. A read-only column shows its value as text, and picking turns the rows into a choice that leaves the unpicked ones out.")),
            new GalleryPage("Selection", "",
                new GallerySection("selection.switch", "Switch", "The track fills with Primary while the thumb slides and grows; the label is part of the hit target."),
                new GallerySection("selection.checkbox", "Checkbox"),
                new GallerySection("selection.radio", "Radio buttons", "Choosing one unchooses the rest of its group."),
                new GallerySection("selection.chips", "Chips",
                    "Assist acts on nearby content, filter toggles and leads with a check while on, input carries a remove button, suggestion offers a reply."),
                new GallerySection("selection.chipcolors", "Chips — custom colours",
                    "Any container and content pair for a semantic chip; a transparent container with an outline makes a brand chip."),
                new GallerySection("selection.status", "Status chips",
                    "A small label tinted by meaning, in two sizes: dense beside a list row, large beside a card title.", Wpf),
                new GallerySection("selection.segmented", "Segmented button",
                    "The choice between a handful of exclusive views, where a dropdown would hide the options and radios would cost a column."),
                new GallerySection("selection.slider", "Slider")),
            new GalleryPage("Cards & Lists", "",
                new GallerySection("cards.header", "Page header",
                    "Overline, title and description, with a slot after the overline, one after the title and the actions on the right."),
                new GallerySection("cards.banner", "Notice banner",
                    "A glyph, a wrapping line, the caller's actions and a dismiss button, tinted by tone. Dismissing collapses it so the content below slides up."),
                new GallerySection("cards.empty", "Empty state", "What a list shows instead of nothing: a muted glyph, a headline, a wrapping explanation and the way out."),
                new GallerySection("cards.expander", "Expander", "A section that folds away under its header; the chevron turns as it opens."),
                new GallerySection("cards.cards", "Card variants", "Elevated · Filled · Outlined."),
                new GallerySection("cards.list", "List items", "One line or two, decided by whether the row carries supporting text."),
                new GallerySection("cards.option", "Option cards", "A card chosen as a whole: radio cards choose one, check cards toggle on their own."),
                new GallerySection("cards.wrap", "Virtualizing wrap panel",
                    "Holds 600 tiles and realizes only the visible rows — the stock WrapPanel realizes every item.", Wpf)),
            new GalleryPage("Progress & Loading", "",
                new GallerySection("progress.linear", "Linear progress", "Determinate, eased to each new value. The slider drives it."),
                new GallerySection("progress.circular", "Circular progress",
                    "Determinate while the fraction is known, indeterminate while only the running is."),
                new GallerySection("progress.skeleton", "Skeleton",
                    "A loading sketch, not a spinner: match the real layout block for block, or use the ready-made card."),
                new GallerySection("progress.busy", "Busy state", "A running action shows a spinner in place of its glyph and ignores clicks until it finishes."),
                new GallerySection("progress.taskbar", "Taskbar progress",
                    "Mirrors work on this window's taskbar button. Each producer opens a session; the oldest open one drives the bar, the next takes over when it ends."),
                new GallerySection("progress.steps", "Step checklist",
                    "The steps of a long operation: done steps carry a check, the running one spins and the rest wait; a failure turns its step red.")),
            new GalleryPage("Navigation", "",
                new GallerySection("nav.tabs", "Tabs", "Primary tabs lead a page and slide a short indicator; secondary tabs sit a level below and underline the whole tab."),
                new GallerySection("nav.badge", "Badge", "A count or a dot on a destination."),
                new GallerySection("nav.destinations", "Navigation rail, bar and drawer",
                    "One selection model in three layouts: exactly one destination is selected, and clicking the selected one keeps it.")),
            new GalleryPage("Overlays & Pickers", "",
                new GallerySection("overlays.tooltip", "Tooltip", "Centred over its control, below it when there is no room above."),
                new GallerySection("overlays.richtooltip", "Rich tooltip", "A heading over the text on a surface container — for a tip that explains rather than names."),
                new GallerySection("overlays.menu", "Menus", "A dropdown opens centred under its trigger and at least as wide; a context menu opens at the pointer."),
                new GallerySection("overlays.select", "Dropdown select", "The picked option in a field; the menu is as wide as the field and the arrow keys move the choice."),
                new GallerySection("overlays.dialog", "Dialog",
                    "Blocks and returns the answer. The tone picks the glyph, a destructive confirm turns red, a held confirm commits only once its fill is full, and a confirm given work spins until the work is done."),
                new GallerySection("overlays.modal", "Modal layer",
                    "A card over a scrim inside the window; the scrim and Esc close it unless the work running inside forbids it.", Wpf),
                new GallerySection("overlays.snackbar", "Snackbar",
                    "The brief confirmation that something happened, with at most one way to act on it. One shows at a time and the rest queue."),
                new GallerySection("overlays.pickers", "Date and time pickers", "Block like a dialog and answer with the value, or nothing for a cancel.")),
            new GalleryPage("Sizes & Text", "Every size, radius, glyph and font the controls draw is a default; these are the same controls with the app's own values.",
                new GallerySection("sizes.percontrol", "Per control", "Each control's own geometry and font, set on that control alone."),
                new GallerySection("sizes.appwide", "App-wide",
                    "One setting every control follows at once: the font family — on WPF a token in the app's resources, on WinForms the type scale's."),
                new GallerySection("sizes.text", "Library text", "The library's own strings follow the UI culture and switch live.")),
        };

        public static GalleryPage Page(string title) =>
            Pages.FirstOrDefault(p => p.Title == title) ?? throw new ArgumentException("No gallery page " + title, nameof(title));

        public static GallerySection Section(string id) =>
            Pages.SelectMany(p => p.Sections).FirstOrDefault(s => s.Id == id) ?? throw new ArgumentException("No gallery section " + id, nameof(id));
    }
}
