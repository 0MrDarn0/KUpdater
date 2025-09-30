using SkiaSharp;

namespace KUpdater.UI {
   public class UICopyrightLabel : IUIElement {
      public string Id { get; }
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();
      public bool Visible { get; set; } = true;
      public string Text { get; set; }
      public Font Font { get; private set; }
      public Color BaseColor { get; set; }

      // 🧩 Skia-Caches
      private SKTypeface? _typeface;
      private SKFont? _skFont;
      private SKPaint? _glowPaint;
      private SKPaint? _gradientPaint;
      private SKShader? _gradientShader;

      private readonly bool _ownsFont;

      public UICopyrightLabel(
          string id,
          Func<Rectangle> boundsFunc,
          string text,
          Font font,
          Color baseColor,
          bool ownsFont = true) {
         Id = id;
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         BaseColor = baseColor;
         _ownsFont = ownsFont;

         InitSkiaResources();
      }

      private void InitSkiaResources() {
         SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
         SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

         _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
         _skFont = new SKFont(_typeface, Font.Size * 1.33f);

         _glowPaint = new SKPaint {
            Color = BaseColor.ToSKColor().WithAlpha(200),
            IsAntialias = true,
            ImageFilter = SKImageFilter.CreateBlur(8, 8)
         };

         // Shader wird dynamisch im Draw() neu gesetzt, weil Bounds gebraucht werden
         _gradientPaint = new SKPaint { IsAntialias = true };
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;
         TextRenderer.DrawText(g, Text, Font, Bounds.Location, BaseColor);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible || _skFont == null || _glowPaint == null || _gradientPaint == null)
            return;

         var bounds = Bounds;
         var metrics = _skFont.Metrics;

         var x = bounds.X;
         var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;

         // Glow
         canvas.DrawText(Text, x, y, _skFont, _glowPaint);

         // Farbverlauf (Shader muss Bounds kennen → hier erzeugen)
         _gradientShader?.Dispose();
         _gradientShader = SKShader.CreateLinearGradient(
             new SKPoint(bounds.Left, bounds.Top),
             new SKPoint(bounds.Right, bounds.Bottom),
             new[] { SKColors.Orange, SKColors.Gold },
             null,
             SKShaderTileMode.Clamp
         );
         _gradientPaint.Shader = _gradientShader;

         canvas.DrawText(Text, x, y, _skFont, _gradientPaint);
      }

      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;

      public void Dispose() {
         if (_ownsFont)
            Font.Dispose();

         _skFont?.Dispose();
         _typeface?.Dispose();
         _glowPaint?.Dispose();
         _gradientPaint?.Dispose();
         _gradientShader?.Dispose();
      }
   }
}
