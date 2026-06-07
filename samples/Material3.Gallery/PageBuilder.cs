using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.Gallery {
    /// <summary>
    /// Stacks demo blocks top-to-bottom inside the scroll panel's content. Width-tracking is
    /// done with anchors so pages reflow on window resize without rebuilding.
    /// </summary>
    internal sealed class PageBuilder {
        private readonly Panel _host;
        private int _y;

        private const int SidePad = 24;

        public PageBuilder(Panel host) {
            _host = host;
            _y = S(Spacing.Space4);
        }

        // Scale a 96-DPI layout value to the host's current DPI so the gallery's own rhythm
        // (gaps, side padding) keeps pace with the DPI-scaled controls.
        private int S(int px) => Dpi.Scale(_host, px);

        /// <summary>Public DPI scale for callers sizing controls they add (e.g. skeleton height).</summary>
        public int Scale(int px) => S(px);

        private int Pad => S(SidePad);

        private int InnerWidth => Math.Max(200, _host.Width - Pad * 2);

        public void Add(Control control) {
            control.Location = new Point(Pad, _y);
            if (control.Width <= 0 || control.Anchor.HasFlag(AnchorStyles.Right)) {
                control.Width = InnerWidth;
            }
            else if (control.Width > InnerWidth || control is MaterialCard || control is RoundedPanel
                || control is MaterialProgressBar || control is StepChecklist || control is MaterialSlider
                || control is DropdownSelect || control is SkeletonCard
                || control is MaterialTabs || control is MaterialNavigationBar
                || control is MaterialListItem || control is MaterialDivider) {
                control.Width = InnerWidth;
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
            _host.Controls.Add(control);
            _y = control.Bottom + S(Spacing.Space3);
        }

        /// <summary>Adds a wrapping flow row and lets the caller fill it with controls.</summary>
        public void Flow(Action<FlowLayoutPanel> fill) {
            var flow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Location = new Point(Pad, _y),
                MaximumSize = new Size(InnerWidth, 0),
            };
            fill(flow);
            // Default margins keep flow children from touching; explicit margins win.
            foreach (Control child in flow.Controls) {
                if (child.Margin == new Padding(3)) {
                    child.Margin = new Padding(0, 0, S(Spacing.Space2), S(Spacing.Space2));
                }
            }
            CenterRow(flow);
            _host.Controls.Add(flow);
            flow.PerformLayout();
            _y = flow.Top + flow.PreferredSize.Height + S(Spacing.Space3);
        }

        // Vertically center a flow row's children on a common mid-line so a row mixing different
        // control heights (e.g. the FAB sizes) reads as one aligned row instead of top-ragged.
        // Equal-height rows are unaffected (the extra space is zero).
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
            _y += S(px) - S(Spacing.Space3);
        }

        public void Header(string text) {
            if (_y > S(Spacing.Space4)) {
                _y += S(Spacing.Space3);
            }
            var label = new SoftLabel {
                Text = text,
                Font = MaterialType.TitleMedium,
                ForeColor = MaterialColors.OnSurface,
                AutoSize = true,
                Location = new Point(Pad, _y),
            };
            _host.Controls.Add(label);
            _y = label.Bottom + S(Spacing.Space1);
        }

        public void Caption(string text) {
            var label = new SoftLabel {
                Text = text,
                Font = MaterialType.BodySmall,
                ForeColor = MaterialColors.OnSurfaceVariant,
                AutoSize = false,
                Width = InnerWidth,
                Height = S(34),
                Location = new Point(Pad, _y),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _host.Controls.Add(label);
            _y = label.Bottom + S(Spacing.Space1);
        }

        public void SwatchGroup(string title, (string name, Color fill, Color content)[] roles) {
            Header(title);
            var flow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Location = new Point(Pad, _y),
                MaximumSize = new Size(InnerWidth, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            foreach ((string name, Color fill, Color content) in roles) {
                flow.Controls.Add(new SwatchTile(name, fill, content));
            }
            _host.Controls.Add(flow);
            // AutoSize height is only valid after a layout pass; PreferredSize gives the wrapped
            // height immediately so the next block doesn't stack on top of the unwrapped rows.
            flow.PerformLayout();
            _y = flow.Top + flow.PreferredSize.Height + S(Spacing.Space3);
        }

        public void TypeSample(string name, TextStyle style) {
            var sample = new TypeSampleControl(name, style) {
                Location = new Point(Pad, _y),
                Width = InnerWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _host.Controls.Add(sample);
            _y = sample.Bottom + S(Spacing.Space1);
        }

        public void ElevationRow() {
            var row = new ElevationRowControl {
                Location = new Point(Pad, _y),
                Width = InnerWidth,
                Height = 130,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _host.Controls.Add(row);
            _y = row.Bottom + S(Spacing.Space3);
        }

        public void ShapeRow() {
            var row = new ShapeRowControl {
                Location = new Point(Pad, _y),
                Width = InnerWidth,
                Height = 96,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _host.Controls.Add(row);
            _y = row.Bottom + S(Spacing.Space3);
        }

        public void ButtonRow(string caption, bool enabled, string? icon = null) {
            Caption(caption);
            var flow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Location = new Point(Pad, _y),
                MaximumSize = new Size(InnerWidth, 0),
            };
            foreach (MaterialButtonVariant variant in new[] {
                MaterialButtonVariant.Elevated, MaterialButtonVariant.Filled, MaterialButtonVariant.Tonal,
                MaterialButtonVariant.Outlined, MaterialButtonVariant.Text,
            }) {
                var button = new MaterialButton {
                    Variant = variant,
                    Text = variant.ToString(),
                    Enabled = enabled,
                    AutoSize = true,
                    // Lock height (floor + ceiling) so every variant sits on one baseline; width
                    // still auto-grows. Min alone left Outlined taller. Scaled so the lock keeps
                    // pace with the DPI-scaled button content instead of clipping it.
                    MinimumSize = new Size(0, S(ComponentSizes.ButtonHeight)),
                    MaximumSize = new Size(0, S(ComponentSizes.ButtonHeight)),
                    Margin = new Padding(0, 0, S(Spacing.Space2), S(Spacing.Space2)),
                };
                if (icon != null) {
                    button.IconGlyph = icon;
                }
                flow.Controls.Add(button);
            }
            _host.Controls.Add(flow);
            flow.PerformLayout();
            _y = flow.Top + flow.PreferredSize.Height + S(Spacing.Space3);
        }

        // ---- demo controls (paint at read-time so theme switches recolor without rebuild) ----

        private sealed class SwatchTile : Control {
            private readonly string _name;
            private readonly Color _fill;
            private readonly Color _content;

            public SwatchTile(string name, Color fill, Color content) {
                _name = name;
                _fill = fill;
                _content = content;
                Size = new Size(212, 64);
                Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedControlRenderer.GetFigurePath(rect, Shape.Medium)) {
                    using (var brush = new SolidBrush(_fill)) {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(MaterialColors.OutlineVariant)) {
                        g.DrawPath(pen, path);
                    }
                }

                string hex = $"#{_fill.R:X2}{_fill.G:X2}{_fill.B:X2}";
                using (var nameBrush = new SolidBrush(_content))
                using (var hexBrush = new SolidBrush(Color.FromArgb(200, _content))) {
                    g.DrawString(_name, MaterialType.LabelMedium, nameBrush, 10, 8);
                    g.DrawString(hex, MaterialType.BodySmall, hexBrush, 10, Height - 26);
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
            }

            // Spec line height + meta row, scaled to DPI: the pt-based sample font renders larger at
            // high DPI, so a raw height clipped the descenders. DeviceDpi needs a handle.
            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                ApplyIntrinsicHeight();
            }

            protected override void OnDpiChangedAfterParent(EventArgs e) {
                base.OnDpiChangedAfterParent(e);
                ApplyIntrinsicHeight();
            }

            private void ApplyIntrinsicHeight() {
                Height = Dpi.Scale(this, Math.Max(_style.LineHeight + 26, 44));
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                string meta = $"{_name}  ·  {_style.Font.SizeInPoints:0.##}pt  ·  ls {_style.LetterSpacing:0.##}px";
                using (var metaBrush = new SolidBrush(MaterialColors.OnSurfaceMuted)) {
                    g.DrawString(meta, MaterialType.LabelSmall, metaBrush, 0, 0);
                }
                MaterialType.DrawString(
                    g,
                    "The quick brown fox",
                    _style,
                    MaterialColors.OnSurface,
                    new PointF(0, Dpi.Scale(this, 16))
                );
            }
        }

        private sealed class ElevationRowControl : Control {
            public ElevationRowControl() {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                int slot = Math.Max(90, Width / 6);
                for (int level = 0; level <= 5; level++) {
                    var rect = new Rectangle(level * slot + 14, 24, Math.Min(96, slot - 24), 64);
                    Elevation.PaintShadow(g, rect, Shape.Medium, level);
                    Color fill = Elevation.TintedSurface(MaterialColors.SurfaceContainerLow, level);
                    using (var path = RoundedControlRenderer.GetFigurePath(rect, Shape.Medium))
                    using (var brush = new SolidBrush(fill)) {
                        g.FillPath(brush, path);
                    }
                    using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                        g.DrawString($"Level {level}", MaterialType.LabelMedium, brush, rect.X + 8, rect.Y + 24);
                    }
                }
            }
        }

        private sealed class ShapeRowControl : Control {
            public ShapeRowControl() {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.Surface);

                (string name, int radius)[] shapes = {
                    ("Xs 4", Shape.ExtraSmall),
                    ("Sm 8", Shape.Small),
                    ("Md 12", Shape.Medium),
                    ("Lg 16", Shape.Large),
                    ("Xl 28", Shape.ExtraLarge),
                    ("Full", Shape.Full),
                };

                int slot = Math.Max(90, Width / shapes.Length);
                for (int i = 0; i < shapes.Length; i++) {
                    var rect = new Rectangle(i * slot + 14, 8, Math.Min(96, slot - 24), 56);
                    using (var path = RoundedControlRenderer.GetFigurePath(rect, shapes[i].radius))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                    using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                        g.DrawString(shapes[i].name, MaterialType.LabelMedium, brush, rect.X + 4, rect.Bottom + 6);
                    }
                }
            }
        }
    }
}
