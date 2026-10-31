using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.Gallery;
using Material3.WinForms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Gallery {
    internal sealed class PageBuilder {
        private sealed class Slot {
            public Control Control = null!;
            public int Before;
            public int After;
        }

        private readonly Panel _host;
        private readonly List<Slot> _slots = new List<Slot>();
        private bool _restacking;

        private const int SidePad = 24;

        public PageBuilder(Panel host) {
            _host = host;
        }

        // Every block is placed by Restack, so one that hides or shrinks (a dismissed banner) closes the page up behind it.
        private void Place(Control control, int after, int before = 0) {
            control.Left = Pad;
            _host.Controls.Add(control);
            _slots.Add(new Slot { Control = control, Before = before, After = after });
            control.SizeChanged += (s, e) => Restack();
            control.VisibleChanged += (s, e) => Restack();
            Restack();
        }

        private void Restack() {
            if (_restacking) {
                return;
            }

            _restacking = true;
            try {
                int y = S(Spacing.Space4);
                foreach (Slot slot in _slots) {
                    Control c = slot.Control;
                    // A hidden host reports every child hidden; until it shows, lay each block out as shown.
                    if (_host.Visible && !c.Visible) {
                        continue;
                    }

                    y += slot.Before;
                    if (c.Top != y) {
                        c.Top = y;
                    }

                    y += (c is FlowLayoutPanel flow ? flow.PreferredSize.Height : c.Height) + slot.After;
                }
            }
            finally {
                _restacking = false;
            }
        }

        private int S(int px) => Dpi.Scale(_host, px);

        public int Scale(int px) => S(px);

        private int Pad => S(SidePad);

        private int InnerWidth => Math.Max(200, _host.Width - Pad * 2);

        // The ids of the sections built so far, in order, for the test that holds both galleries to one spec.
        public List<string> Emitted { get; } = new List<string>();

        public void Add(Control control) {
            if (control.Width <= 0 || control.Width > InnerWidth || control.Anchor.HasFlag(AnchorStyles.Right)) {
                control.Width = InnerWidth;
            }
            Place(control, S(Spacing.Space3));
        }

        public void PageTitle(GalleryPage page) {
            var title = new MaterialLabel {
                Text = page.Title,
                Font = TypeScale.TitleLarge,
                ForeColor = MaterialColors.OnSurface,
                AutoSize = true,
            };
            Place(title, S(Spacing.Space1));
            if (page.Caption.Length > 0) {
                Caption(page.Caption);
            }
        }

        public void Section(GallerySection section) {
            Emitted.Add(section.Id);
            Header(section.Title);
            if (section.Caption.Length > 0) {
                Caption(section.Caption);
            }
        }

        public void Flow(Action<FlowLayoutPanel> fill, int maxWidth = 0) {
            var flow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                MaximumSize = new Size(maxWidth > 0 ? Math.Min(S(maxWidth), InnerWidth) : InnerWidth, 0),
            };
            fill(flow);
            foreach (Control child in flow.Controls) {
                if (child.Margin == new Padding(3)) {
                    child.Margin = new Padding(0, 0, S(Spacing.Space2), S(Spacing.Space2));
                }
            }
            CenterRow(flow);
            flow.PerformLayout();
            Place(flow, S(Spacing.Space3));
        }

        // A row of mixed heights (the FAB sizes) centres on one mid-line
        private static void CenterRow(FlowLayoutPanel flow) {
            int rowHeight = 0;
            foreach (Control child in flow.Controls) {
                rowHeight = Math.Max(rowHeight, child.Height);
            }
            foreach (Control child in flow.Controls) {
                int extra = rowHeight - child.Height;
                if (extra <= 0) {
                    continue;
                }
                int top = extra / 2;
                Padding m = child.Margin;
                child.Margin = new Padding(m.Left, m.Top + top, m.Right, m.Bottom + (extra - top));
            }
        }

        public void Gap(int px) {
            _slots[_slots.Count - 1].After += S(px) - S(Spacing.Space3);
            Restack();
        }

        public void Header(string text) {
            var label = new MaterialLabel {
                Text = text,
                Font = TypeScale.TitleMedium,
                ForeColor = MaterialColors.OnSurface,
                AutoSize = true,
            };
            Place(label, S(Spacing.Space1), before: _slots.Count > 0 ? S(Spacing.Space3) : 0);
        }

        public void Caption(string text) {
            int height = TextRenderer.MeasureText(text, TypeScale.BodySmall, new Size(InnerWidth, int.MaxValue), TextFormatFlags.WordBreak).Height;
            var label = new MaterialLabel {
                Text = text,
                Font = TypeScale.BodySmall,
                ForeColor = MaterialColors.OnSurfaceVariant,
                AutoSize = false,
                Width = InnerWidth,
                Height = height + S(4),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Place(label, S(Spacing.Space1));
        }

        public void Swatches((string name, Func<Color> fill, Func<Color> content)[] roles) {
            var flow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                MaximumSize = new Size(InnerWidth, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            foreach ((string name, Func<Color> fill, Func<Color> content) in roles) {
                flow.Controls.Add(new SwatchTile(name, fill, content));
            }
            flow.PerformLayout();
            Place(flow, S(Spacing.Space3));
        }

        public void TypeSample(string name, TextStyle style) {
            var sample = new TypeSampleControl(name, style) {
                Width = InnerWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Place(sample, S(Spacing.Space1));
        }

        public void ElevationRow() {
            var row = new ElevationRowControl {
                Width = InnerWidth,
                Height = S(100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Place(row, S(Spacing.Space3));
        }

        public void ShapeRow() {
            var row = new ShapeRowControl {
                Width = InnerWidth,
                Height = S(84),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Place(row, S(Spacing.Space3));
        }


        private sealed class SwatchTile : Control {
            private readonly string _name;
            private readonly Func<Color> _fill;
            private readonly Func<Color> _content;

            public SwatchTile(string name, Func<Color> fill, Func<Color> content) {
                _name = name;
                _fill = fill;
                _content = content;
                Size = new Size(212, 64);
                Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                ThemeHook.Attach(this, Invalidate);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                Color fill = _fill();
                Color content = _content();
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Medium)) {
                    using (var brush = new SolidBrush(fill)) {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(MaterialColors.OutlineVariant)) {
                        g.DrawPath(pen, path);
                    }
                }

                string hex = $"#{fill.R:X2}{fill.G:X2}{fill.B:X2}";
                using (var nameBrush = new SolidBrush(content))
                using (var hexBrush = new SolidBrush(Color.FromArgb(200, content))) {
                    g.DrawString(_name, TypeScale.LabelMedium, nameBrush, 10, 8);
                    g.DrawString(hex, TypeScale.BodySmall, hexBrush, 10, Height - 26);
                }
            }
        }

        private sealed class TypeSampleControl : Control {
            private readonly string _name;
            private readonly TextStyle _style;

            public TypeSampleControl(string name, TextStyle style) {
                _name = name;
                _style = style;
                ApplyIntrinsicHeight();
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                ThemeHook.Attach(this, Invalidate);
            }

            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                ApplyIntrinsicHeight();
            }

            protected override void OnDpiChangedAfterParent(EventArgs e) {
                base.OnDpiChangedAfterParent(e);
                ApplyIntrinsicHeight();
            }

            private void ApplyIntrinsicHeight() {
                // The real font height, not the spec line height: big Segoe sizes clip their descenders otherwise
                Height = Dpi.Scale(this, Math.Max(16 + _style.Font.Height + 8, 44));
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                string meta = $"{_name}  ·  {_style.Font.SizeInPoints * 96f / 72f:0.##}px";
                using (var metaBrush = new SolidBrush(MaterialColors.OnSurfaceMuted)) {
                    g.DrawString(meta, TypeScale.LabelSmall, metaBrush, 0, 0);
                }
                TypeScale.DrawString(
                    g,
                    GallerySpec.TypeSample,
                    _style,
                    MaterialColors.OnSurface,
                    new PointF(0, Dpi.Scale(this, 16))
                );
            }
        }

        private const int TileGap = 20;

        private static void DrawTileLabel(Graphics g, string text, Rectangle tile, Color color) =>
            TextRenderer.DrawText(g, text, TypeScale.LabelMedium, tile, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        private sealed class ElevationRowControl : Control {
            public ElevationRowControl() {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                ThemeHook.Attach(this, Invalidate);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(MaterialColors.Surface);

                int w = Dpi.Scale(this, 96), h = Dpi.Scale(this, 72), step = w + Dpi.Scale(this, TileGap);
                for (int level = 0; level <= 5; level++) {
                    var rect = new Rectangle(level * step, Dpi.Scale(this, 4), w, h);
                    int radius = Dpi.Scale(this, Shape.Medium);
                    Elevation.PaintShadow(g, rect, radius, level);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, radius))
                    using (var brush = new SolidBrush(Elevation.TintedSurface(MaterialColors.Surface, level))) {
                        g.FillPath(brush, path);
                    }
                    DrawTileLabel(g, "Level " + level, rect, MaterialColors.OnSurface);
                }
            }
        }

        private sealed class ShapeRowControl : Control {
            public ShapeRowControl() {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                ThemeHook.Attach(this, Invalidate);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(MaterialColors.Surface);

                (string name, int radius)[] shapes = {
                    ("Xs 4", Shape.ExtraSmall),
                    ("Sm 8", Shape.Small),
                    ("Md 12", Shape.Medium),
                    ("Lg 16", Shape.Large),
                    ("Pill 20", Shape.LargeIncreased),
                    ("Xl 28", Shape.ExtraLarge),
                    ("Full", Shape.Full),
                };

                int gap = Dpi.Scale(this, 12), h = Dpi.Scale(this, 72);
                int w = Math.Max(1, Math.Min(Dpi.Scale(this, 120), (Width - gap * (shapes.Length - 1)) / shapes.Length));
                int step = w + gap;
                for (int i = 0; i < shapes.Length; i++) {
                    var rect = new Rectangle(i * step, 0, w, h);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Math.Min(Dpi.Scale(this, shapes[i].radius), h / 2)))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                    DrawTileLabel(g, shapes[i].name, rect, MaterialColors.OnSecondaryContainer);
                }
            }
        }
    }
}
