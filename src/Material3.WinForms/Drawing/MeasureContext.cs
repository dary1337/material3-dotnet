using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material3.WinForms.Drawing {
    // CreateGraphics makes a window handle, which a layout pass over a control nobody shows must not do.
    internal sealed class MeasureContext : IDisposable {
        private readonly Bitmap? _bitmap;

        private MeasureContext(Graphics graphics, Bitmap? bitmap) {
            Graphics = graphics;
            _bitmap = bitmap;
        }

        public Graphics Graphics { get; }

        public static MeasureContext For(Control control) {
            if (control.IsHandleCreated) {
                return new MeasureContext(control.CreateGraphics(), null);
            }

            var bitmap = new Bitmap(1, 1);
            try {
                bitmap.SetResolution(control.DeviceDpi, control.DeviceDpi);
                return new MeasureContext(Graphics.FromImage(bitmap), bitmap);
            }
            catch {
                bitmap.Dispose();
                throw;
            }
        }

        public void Dispose() {
            Graphics.Dispose();
            _bitmap?.Dispose();
        }
    }
}
