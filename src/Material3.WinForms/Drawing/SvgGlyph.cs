using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Material3.WinForms.Drawing {
    /// <summary>
    /// Minimal SVG-path → <see cref="GraphicsPath"/> parser: M/L/H/V/Q/T/C/S/Z (absolute + relative),
    /// quadratic/cubic Béziers and the viewBox transform. No arcs, gradients, transforms or CSS.
    /// </summary>
    internal static class SvgGlyph {
        private static readonly Regex ViewBoxRx = new Regex("viewBox=\"([^\"]*)\"", RegexOptions.Compiled);
        private static readonly Regex PathRx = new Regex("\\sd=\"([^\"]*)\"", RegexOptions.Compiled);

        /// <summary>
        /// Parses SVG text into a path in viewBox coordinates plus the viewBox rectangle, or null if
        /// none found. FillMode is Winding (the SVG default).
        /// </summary>
        internal static (GraphicsPath path, RectangleF viewBox)? Parse(string svg) {
            Match vb = ViewBoxRx.Match(svg);
            RectangleF viewBox = vb.Success ? ParseViewBox(vb.Groups[1].Value) : new RectangleF(0, 0, 24, 24);

            var path = new GraphicsPath { FillMode = FillMode.Winding };
            bool any = false;
            foreach (Match m in PathRx.Matches(svg)) {
                if (AppendPathData(path, m.Groups[1].Value)) {
                    any = true;
                }
            }
            if (!any) {
                path.Dispose();
                return null;
            }
            return (path, viewBox);
        }

        private static RectangleF ParseViewBox(string value) {
            string[] parts = value.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) {
                return new RectangleF(0, 0, 24, 24);
            }
            return new RectangleF(F(parts[0]), F(parts[1]), F(parts[2]), F(parts[3]));
        }

        private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        private static bool AppendPathData(GraphicsPath path, string d) {
            int i = 0;
            char cmd = '\0';
            char lastCmd = '\0';
            double curX = 0, curY = 0, startX = 0, startY = 0, ctrlX = 0, ctrlY = 0;
            bool open = false;
            bool added = false;

            while (true) {
                SkipSeparators(d, ref i);
                if (i >= d.Length) {
                    break;
                }
                if (char.IsLetter(d[i])) {
                    cmd = d[i];
                    i++;
                    if (cmd == 'Z' || cmd == 'z') {
                        path.CloseFigure();
                        open = false;
                        curX = startX;
                        curY = startY;
                        lastCmd = cmd;
                        continue;
                    }
                }

                bool rel = char.IsLower(cmd);
                switch (char.ToUpperInvariant(cmd)) {
                    case 'M': {
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { x += curX; y += curY; }
                        curX = startX = x; curY = startY = y;
                        path.StartFigure();
                        open = true;
                        // Subsequent coordinate pairs after a moveto are implicit lineto.
                        cmd = rel ? 'l' : 'L';
                        break;
                    }
                    case 'L': {
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { x += curX; y += curY; }
                        EnsureOpen(path, ref open);
                        path.AddLine((float)curX, (float)curY, (float)x, (float)y);
                        curX = x; curY = y; added = true;
                        break;
                    }
                    case 'H': {
                        double x = Num(d, ref i);
                        if (rel) { x += curX; }
                        EnsureOpen(path, ref open);
                        path.AddLine((float)curX, (float)curY, (float)x, (float)curY);
                        curX = x; added = true;
                        break;
                    }
                    case 'V': {
                        double y = Num(d, ref i);
                        if (rel) { y += curY; }
                        EnsureOpen(path, ref open);
                        path.AddLine((float)curX, (float)curY, (float)curX, (float)y);
                        curY = y; added = true;
                        break;
                    }
                    case 'Q': {
                        double cx = Num(d, ref i), cy = Num(d, ref i);
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { cx += curX; cy += curY; x += curX; y += curY; }
                        EnsureOpen(path, ref open);
                        AddQuad(path, curX, curY, cx, cy, x, y);
                        ctrlX = cx; ctrlY = cy; curX = x; curY = y; added = true;
                        break;
                    }
                    case 'T': {
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { x += curX; y += curY; }
                        // Smooth quadratic: reflect the previous control point about the current
                        // point when the previous command was also a quadratic; else use current.
                        double cx, cy;
                        if (char.ToUpperInvariant(lastCmd) == 'Q' || char.ToUpperInvariant(lastCmd) == 'T') {
                            cx = 2 * curX - ctrlX; cy = 2 * curY - ctrlY;
                        }
                        else {
                            cx = curX; cy = curY;
                        }
                        EnsureOpen(path, ref open);
                        AddQuad(path, curX, curY, cx, cy, x, y);
                        ctrlX = cx; ctrlY = cy; curX = x; curY = y; added = true;
                        break;
                    }
                    case 'C': {
                        double c1x = Num(d, ref i), c1y = Num(d, ref i);
                        double c2x = Num(d, ref i), c2y = Num(d, ref i);
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { c1x += curX; c1y += curY; c2x += curX; c2y += curY; x += curX; y += curY; }
                        EnsureOpen(path, ref open);
                        path.AddBezier((float)curX, (float)curY, (float)c1x, (float)c1y, (float)c2x, (float)c2y, (float)x, (float)y);
                        ctrlX = c2x; ctrlY = c2y; curX = x; curY = y; added = true;
                        break;
                    }
                    case 'S': {
                        double c2x = Num(d, ref i), c2y = Num(d, ref i);
                        double x = Num(d, ref i), y = Num(d, ref i);
                        if (rel) { c2x += curX; c2y += curY; x += curX; y += curY; }
                        // Smooth cubic: reflect the previous cubic's 2nd control point about the
                        // current point when the previous command was also a cubic; else use current.
                        double c1x, c1y;
                        if (char.ToUpperInvariant(lastCmd) == 'C' || char.ToUpperInvariant(lastCmd) == 'S') {
                            c1x = 2 * curX - ctrlX; c1y = 2 * curY - ctrlY;
                        }
                        else {
                            c1x = curX; c1y = curY;
                        }
                        EnsureOpen(path, ref open);
                        path.AddBezier((float)curX, (float)curY, (float)c1x, (float)c1y, (float)c2x, (float)c2y, (float)x, (float)y);
                        ctrlX = c2x; ctrlY = c2y; curX = x; curY = y; added = true;
                        break;
                    }
                    default:
                        // Unsupported command (e.g. arcs) — stop this subpath.
                        return added;
                }
                lastCmd = cmd;
            }
            return added;
        }

        private static void EnsureOpen(GraphicsPath path, ref bool open) {
            if (!open) {
                path.StartFigure();
                open = true;
            }
        }

        // Quadratic (P0, C, P2) → cubic, exact: C1 = P0 + 2/3(C-P0), C2 = P2 + 2/3(C-P2).
        private static void AddQuad(GraphicsPath path, double x0, double y0, double cx, double cy, double x2, double y2) {
            double c1x = x0 + 2.0 / 3.0 * (cx - x0);
            double c1y = y0 + 2.0 / 3.0 * (cy - y0);
            double c2x = x2 + 2.0 / 3.0 * (cx - x2);
            double c2y = y2 + 2.0 / 3.0 * (cy - y2);
            path.AddBezier((float)x0, (float)y0, (float)c1x, (float)c1y, (float)c2x, (float)c2y, (float)x2, (float)y2);
        }

        private static void SkipSeparators(string s, ref int i) {
            while (i < s.Length) {
                char c = s[i];
                if (c == ' ' || c == ',' || c == '\t' || c == '\n' || c == '\r') {
                    i++;
                }
                else {
                    break;
                }
            }
        }

        // Reads one SVG number; a '+'/'-' that isn't the first char (and isn't an exponent sign)
        // begins the next number, so "382-354" tokenizes as 382 then -354.
        private static double Num(string s, ref int i) {
            SkipSeparators(s, ref i);
            int start = i;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) {
                i++;
            }
            while (i < s.Length && char.IsDigit(s[i])) {
                i++;
            }
            if (i < s.Length && s[i] == '.') {
                i++;
                while (i < s.Length && char.IsDigit(s[i])) {
                    i++;
                }
            }
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E')) {
                i++;
                if (i < s.Length && (s[i] == '+' || s[i] == '-')) {
                    i++;
                }
                while (i < s.Length && char.IsDigit(s[i])) {
                    i++;
                }
            }
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }
    }
}
