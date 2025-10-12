// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Utility;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI.Control {

    [ExposeToLua]
    public class Button : IControl {
        public string Id { get; }
        private readonly Func<Rectangle> _boundsFunc;
        public Rectangle Bounds => _boundsFunc();
        public string Text { get; set; }
        public Font Font { get; set; }
        public Color Color { get; set; }
        public string ThemeKey { get; set; }
        public Action? OnClick { get; set; }
        public bool Visible { get; set; } = true;
        public bool IsHovered { get; private set; }
        public bool IsPressed { get; private set; }
        private readonly bool _ownsFont;
        private bool _disposed;

        // 🧩 Cache für Bilder
        private readonly Dictionary<string, SKBitmap> _stateBitmaps = [];
        private readonly Dictionary<string, Image> _stateImages = [];

        // 🧩 paint,font and Typeface cachen
        private SKTypeface? _typeface;
        private SKFont? _skFont;
        private SKPaint? _skPaint;

        public Button(string id, Func<Rectangle> boundsFunc, string text, Font font, Color color, string themeKey, Action? onClick, bool ownsFont = true) {
            Id = id;
            _boundsFunc = boundsFunc;
            Text = text;
            Font = font;
            _ownsFont = ownsFont;
            Color = color;
            ThemeKey = themeKey;
            OnClick = onClick;

            LoadResources();
        }

        public Button(string id, Table bounds, string text, Font font, Color color,
                        string themeKey, Action? onClick, bool ownsFont = true)
            : this(id, () => new Rectangle(
                (int)(bounds.Get("x").CastToNumber() ?? 0),
                (int)(bounds.Get("y").CastToNumber() ?? 0),
                (int)(bounds.Get("width").CastToNumber() ?? 0),
                (int)(bounds.Get("height").CastToNumber() ?? 0)
            ), text, font, color, themeKey, onClick, ownsFont) {
        }


        private void LoadResources() {
            foreach (var state in new[] { "normal", "hover", "click" }) {
                string path = Paths.Resource($"{ThemeKey}/{Id}_{state}.png");
                if (File.Exists(path)) {
                    _stateImages[state] = Image.FromFile(path);
                    _stateBitmaps[state] = SKBitmap.Decode(path);
                }
            }

            SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
            _skFont = new SKFont(_typeface, Font.Size * 1.33f);
            _skPaint = new SKPaint { Color = Color.ToSKColor(), IsAntialias = true };
        }

        public void Draw(Graphics g) {
            if (!Visible)
                return;

            string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
            if (_stateImages.TryGetValue(state, out var img)) {
                g.DrawImage(img, Bounds);
            }

            TextRenderer.DrawText(g, Text, Font, Bounds, Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public void Draw(SKCanvas canvas) {
            if (!Visible)
                return;

            string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
            if (_stateBitmaps.TryGetValue(state, out var img)) {
                var bounds = Bounds;
                var destRect = new SKRect(bounds.X, bounds.Y, bounds.Right, bounds.Bottom);
                canvas.DrawBitmap(img, destRect);
            }

            var metrics = _skFont!.Metrics;
            var x = Bounds.X + Bounds.Width / 2;
            var y = Bounds.Y + Bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2 - metrics.Descent * 0.3f;

            canvas.DrawText(Text, x, y, SKTextAlign.Center, _skFont, _skPaint!);
        }

        public bool OnMouseMove(Point p) {
            bool prev = IsHovered;
            IsHovered = Bounds.Contains(p);
            return prev != IsHovered;
        }

        public bool OnMouseDown(Point p) {
            bool prev = IsPressed;
            IsPressed = Bounds.Contains(p);
            return prev != IsPressed;
        }

        public bool OnMouseUp(Point p) {
            bool prevPressed = IsPressed;
            if (IsPressed && Bounds.Contains(p))
                OnClick?.Invoke();
            IsPressed = false;
            return prevPressed != IsPressed;
        }

        public bool OnMouseWheel(int delta, Point p) => false;


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

                foreach (var img in _stateImages.Values)
                    img.Dispose();
                foreach (var bmp in _stateBitmaps.Values)
                    bmp.Dispose();

                _typeface?.Dispose();
                _skPaint?.Dispose();
                _skFont?.Dispose();
            }

            // Unmanaged Ressourcen hier freigeben
            _disposed = true;
        }
    }
}
