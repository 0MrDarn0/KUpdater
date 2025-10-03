// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text;
using KUpdater.Scripting;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI {

    [ExposeToLua]
    public class UITextBox : IUIElement {
        public string Id { get; }
        private readonly Func<Rectangle> _boundsFunc;
        public Rectangle Bounds => _boundsFunc();

        public string Text { get; set; }
        public Font Font { get; }
        public Color ForeColor { get; set; }
        public Color BackColor { get; set; }
        public bool Visible { get; set; } = true;
        public bool ReadOnly { get; set; }
        public bool Multiline { get; set; }
        public Color ScrollBarColor { get; set; } = Color.FromArgb(80, 80, 80);

        private readonly bool _ownsFont;
        // Skia Ressourcen
        private SKTypeface? _typeface;
        private SKFont? _skFont;
        private SKPaint? _textPaint;
        private SKPaint? _bgPaint;

        private int _scrollOffset = 0;   // in Pixeln
        private int _lineHeight;

        private bool _isDraggingMarker;
        private int _dragStartY;
        private int _scrollStartOffset;
        private SKRect _markerRect;
        private bool _disposed;

        public Color BorderColor { get; set; } = Color.Gold;
        public int BorderThickness { get; set; } = 2;
        public bool GlowEnabled { get; set; } = true;
        public Color GlowColor { get; set; } = Color.Gold;
        public float GlowRadius { get; set; } = 8f;


        public UITextBox(string id, Func<Rectangle> boundsFunc, string text, Font font,
                         Color foreColor, Color backColor,
                         bool multiline = true, bool readOnly = false, Color? scrollBarColor = null, bool ownsFont = true) {
            Id = id;
            _boundsFunc = boundsFunc;
            Text = text;
            Font = font;
            ForeColor = foreColor;
            BackColor = backColor;
            Multiline = multiline;
            ReadOnly = readOnly;
            if (scrollBarColor.HasValue)
                ScrollBarColor = scrollBarColor.Value;
            _ownsFont = ownsFont;
            InitSkiaResources();
        }

        public UITextBox(string id, Table bounds, string text, Font font, Color foreColor, Color backColor,
                         bool multiline = true, bool readOnly = false, Color? scrollBarColor = null, bool ownsFont = true)
            : this(id, () => new Rectangle(
                (int)(bounds.Get("x").CastToNumber() ?? 0),
                (int)(bounds.Get("y").CastToNumber() ?? 0),
                (int)(bounds.Get("width").CastToNumber() ?? 0),
                (int)(bounds.Get("height").CastToNumber() ?? 0)
            ), text, font, foreColor, backColor, multiline, readOnly, scrollBarColor, ownsFont) { }


        private void InitSkiaResources() {
            SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

            _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));

            _skFont = new SKFont(_typeface, Font.Size * 1.33f);
            _textPaint = new SKPaint { Color = ForeColor.ToSKColor(), IsAntialias = true };
            _bgPaint = new SKPaint { Color = BackColor.ToSKColor(), IsAntialias = true, Style = SKPaintStyle.Fill };
        }

        public void Draw(Graphics g) {
            if (!Visible)
                return;

            // Optional: Fallback GDI+ Rendering
            g.FillRectangle(new SolidBrush(BackColor), Bounds);
            TextRenderer.DrawText(g, Text, Font, Bounds, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
        }

        public void Draw(SKCanvas canvas) {
            if (!Visible || _skFont == null || _textPaint == null || _bgPaint == null)
                return;

            var rect = Bounds;

            // Hintergrund
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _bgPaint);

            // --- Rahmen & Glow ---
            if (BorderThickness > 0) {
                using var borderPaint = new SKPaint {
                    Color = BorderColor.ToSKColor(),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = BorderThickness
                };

                // Glow zuerst (unter dem Rahmen)
                if (GlowEnabled) {
                    using var glowPaint = new SKPaint {
                        Color = GlowColor.ToSKColor().WithAlpha(180),
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = BorderThickness,
                        ImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius)
                    };
                    canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, glowPaint);
                }

                // Rahmen
                canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, borderPaint);
            }


            // Clipping aktivieren
            canvas.Save();
            canvas.ClipRect(new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom));

            var metrics = _skFont.Metrics;
            _lineHeight = (int)((metrics.Descent - metrics.Ascent) * 1.2f); // Zeilenhöhe

            float x = rect.X + 4;

            // Start-Y: obere Kante + Abstand bis zur Baseline
            float y = rect.Y - metrics.Ascent - _scrollOffset;

            if (Multiline) {
                foreach (var paragraph in Text.Split(["\r\n", "\n"], StringSplitOptions.None)) {
                    var words = paragraph.Split(' ');
                    var lineBuilder = new StringBuilder();

                    foreach (var word in words) {
                        string testLine = lineBuilder.Length == 0 ? word : lineBuilder.ToString() + " " + word;
                        float width = _skFont.MeasureText(testLine, out _);

                        if (x + width > rect.Right - 8) {
                            canvas.DrawText(lineBuilder.ToString(), x, y, SKTextAlign.Left, _skFont, _textPaint);
                            y += _lineHeight;
                            lineBuilder.Clear();
                            lineBuilder.Append(word);
                        } else {
                            lineBuilder.Clear();
                            lineBuilder.Append(testLine);
                        }
                    }

                    if (lineBuilder.Length > 0) {
                        canvas.DrawText(lineBuilder.ToString(), x, y, SKTextAlign.Left, _skFont, _textPaint);
                        y += _lineHeight;
                    }
                }
            } else {
                canvas.DrawText(Text, x, y, SKTextAlign.Left, _skFont, _textPaint);
            }


            // Clipping wiederherstellen
            canvas.Restore();

            // --- Scrollbar / Marker ---
            // Nur zeichnen, wenn der Text höher ist als der sichtbare Bereich
            float totalHeight = GetTotalTextHeight();
            if (totalHeight > Bounds.Height) {
                float visibleHeight = Bounds.Height;
                float ratio = visibleHeight / totalHeight;
                float markerHeight = Math.Max(20, visibleHeight * ratio);
                float maxScroll = totalHeight - visibleHeight;

                // ScrollOffset begrenzen
                ClampScrollOffset(maxScroll);

                // maxScroll könnte 0 sein (kein Scrollen möglich) -> Marker nicht zeichnen in dem Fall (division by zero)
                if (maxScroll > 0) {
                    using var markerPaint = new SKPaint { Color = ScrollBarColor.ToSKColor().WithAlpha(160), IsAntialias = true };

                    // Position des Markers basierend auf dem Scroll-Offset
                    float markerY = Bounds.Y + (_scrollOffset / maxScroll) * (visibleHeight - markerHeight);

                    _markerRect = new SKRect(Bounds.Right - 6, markerY, Bounds.Right - 2, markerY + markerHeight);
                    canvas.DrawRect(_markerRect, markerPaint);
                }
            }
        }

        public bool OnMouseDown(Point p) {
            if (_markerRect.Contains(p.X, p.Y)) {
                _isDraggingMarker = true;
                _dragStartY = p.Y;
                _scrollStartOffset = _scrollOffset;
                return true;
            }
            // --- Jump to clicked scrollbar position ---
            if (p.X >= Bounds.Right - 8 && p.X <= Bounds.Right) {
                float totalHeight = GetTotalTextHeight();
                float visibleHeight = Bounds.Height;
                float maxScroll = totalHeight - visibleHeight;

                if (maxScroll > 0) {
                    float clickRatio = (float)(p.Y - Bounds.Y) / visibleHeight;
                    _scrollOffset = (int)(clickRatio * maxScroll);
                    ClampScrollOffset(maxScroll);
                    return true;
                }
            }
            return false;
        }

        public bool OnMouseMove(Point p) {
            if (_isDraggingMarker) {
                float totalHeight = GetTotalTextHeight();
                float visibleHeight = Bounds.Height;
                float markerHeight = Math.Max(20, visibleHeight * (visibleHeight / totalHeight));
                float maxScroll = totalHeight - visibleHeight;

                float delta = p.Y - _dragStartY;
                float scrollRatio = delta / (visibleHeight - markerHeight);

                _scrollOffset = (int)(_scrollStartOffset + scrollRatio * maxScroll);
                ClampScrollOffset(maxScroll);
                return true;
            }
            return false;
        }

        public bool OnMouseUp(Point p) {
            _isDraggingMarker = false;
            return false;
        }

        public bool OnMouseWheel(int delta, Point p) {
            if (!Bounds.Contains(p))
                return false;

            int scrollStep = _lineHeight * 3;
            _scrollOffset -= Math.Sign(delta) * scrollStep;

            float maxScroll = Math.Max(0, GetTotalTextHeight() - Bounds.Height);
            ClampScrollOffset(maxScroll);

            return true;
        }

        private void ClampScrollOffset(float maxScroll) {
            if (_scrollOffset < 0)
                _scrollOffset = 0;
            if (_scrollOffset > maxScroll)
                _scrollOffset = (int)maxScroll;
        }

        private float GetTotalTextHeight() {
            int lines = Text.Split(["\r\n", "\n"], StringSplitOptions.None).Length;
            return lines * _lineHeight;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this); // verhindert unnötigen Finalizer
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed)
                return;

            if (disposing) {
                // Managed Ressourcen freigeben
                if (_ownsFont)
                    Font.Dispose();

                _textPaint?.Dispose();
                _bgPaint?.Dispose();
                _skFont?.Dispose();
                _typeface?.Dispose();

                _textPaint = null;
                _bgPaint = null;
                _skFont = null;
                _typeface = null;
            }

            // Unmanaged Ressourcen hier freigeben
            _disposed = true;
        }

    }
}
