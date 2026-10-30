using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>The meaning a <see cref="MaterialNoticeBanner"/> is tinted with.</summary>
    public enum MaterialTone {
        /// <summary>No particular meaning: surface colours.</summary>
        Neutral,
        /// <summary>A positive or informational note in the secondary accent.</summary>
        Accent,
        /// <summary>Something finished well.</summary>
        Success,
        /// <summary>Something needs attention.</summary>
        Warning,
        /// <summary>Something failed.</summary>
        Error,
    }

    internal static class ToneColors {
        public static (Color container, Color content) Of(MaterialTone tone) {
            switch (tone) {
                case MaterialTone.Accent: return (MaterialColors.SecondaryContainer, MaterialColors.OnSecondaryContainer);
                case MaterialTone.Success: return (MaterialColors.SuccessContainer, MaterialColors.OnSuccessContainer);
                case MaterialTone.Warning: return (MaterialColors.WarningContainer, MaterialColors.OnWarningContainer);
                case MaterialTone.Error: return (MaterialColors.ErrorContainer, MaterialColors.OnErrorContainer);
                default: return (MaterialColors.SurfaceContainerHigh, MaterialColors.OnSurface);
            }
        }
    }
}
