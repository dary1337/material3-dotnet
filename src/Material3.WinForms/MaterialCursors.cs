using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Material3.WinForms {
    /// <summary>
    /// Cursors loaded live from the OS via LoadCursor, so they follow the user's current cursor
    /// scheme/size. The <see cref="Cursors"/> statics ship legacy baked-in bitmaps on .NET Framework
    /// and ignore the user's scheme. Held static (never disposed) — they wrap shared OS handles.
    /// </summary>
    public static class MaterialCursors {
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int cursorName);

        private static Cursor Load(int idc) => new Cursor(LoadCursor(IntPtr.Zero, idc));

        /// <summary>Standard arrow.</summary>
        public static readonly Cursor Arrow = Load(32512);
        /// <summary>Text I-beam, for text-input controls.</summary>
        public static readonly Cursor IBeam = Load(32513);
        /// <summary>Busy / hourglass.</summary>
        public static readonly Cursor Wait = Load(32514);
        /// <summary>Crosshair.</summary>
        public static readonly Cursor Cross = Load(32515);
        /// <summary>Vertical up-arrow.</summary>
        public static readonly Cursor UpArrow = Load(32516);
        /// <summary>Diagonal resize ↖↘.</summary>
        public static readonly Cursor SizeNWSE = Load(32642);
        /// <summary>Diagonal resize ↗↙.</summary>
        public static readonly Cursor SizeNESW = Load(32643);
        /// <summary>Horizontal resize ↔.</summary>
        public static readonly Cursor SizeWE = Load(32644);
        /// <summary>Vertical resize ↕.</summary>
        public static readonly Cursor SizeNS = Load(32645);
        /// <summary>Move / four-way resize.</summary>
        public static readonly Cursor SizeAll = Load(32646);
        /// <summary>Unavailable / "no drop".</summary>
        public static readonly Cursor No = Load(32648);
        /// <summary>Hand pointer, for clickable controls.</summary>
        public static readonly Cursor Pointer = Load(32649);
        /// <summary>Arrow with a small hourglass (background work starting).</summary>
        public static readonly Cursor AppStarting = Load(32650);
        /// <summary>Arrow with a question mark.</summary>
        public static readonly Cursor Help = Load(32651);

        /// <summary>
        /// Assigns a Material cursor at run time only. In the designer it's skipped: these wrap raw OS
        /// handles that <see cref="System.ComponentModel.TypeConverter"/> for <see cref="Cursor"/>
        /// can't serialize into InitializeComponent, which would otherwise break code generation for
        /// any form hosting the control. Leaving the design-time cursor at its default avoids that.
        /// </summary>
        public static void Apply(Control control, Cursor cursor) {
            if (control != null && !Theming.DesignTime.Active) {
                control.Cursor = cursor;
            }
        }
    }
}
