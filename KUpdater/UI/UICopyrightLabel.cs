using SkiaSharp;

namespace KUpdater.UI {
   public class UICopyrightLabel : IUIElement {
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();
      public bool Visible { get; set; } = true;
      public string Text { get; set; }
      public Font Font { get; set; }
      public Color BaseColor { get; set; }

      public UICopyrightLabel(Func<Rectangle> boundsFunc, string text, Font font, Color baseColor) {
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         BaseColor = baseColor;
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;
         TextRenderer.DrawText(g, Text, Font, Bounds.Location, BaseColor);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible)
            return;

         var bounds = Bounds;
         float fontSize = Font.Size * 1.33f;

         SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
         SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

         var typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));

         using var font = new SKFont {
            Typeface = typeface,
            Size = fontSize
         };

         var metrics = font.Metrics;
         var x = bounds.X;
         var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;

         //// Schatten
         //using var shadowPaint = new SKPaint {
         //   Color = SKColors.White.WithAlpha(100),
         //   IsAntialias = true
         //};
         //canvas.DrawText(Text, x + 1, y + 1, font, shadowPaint);

         // Glow
         using var glowPaint = new SKPaint {
            Color = BaseColor.ToSKColor().WithAlpha(200),
            IsAntialias = true,
            ImageFilter = SKImageFilter.CreateBlur(8, 8)
         };
         canvas.DrawText(Text, x, y, font, glowPaint);

         // Farbverlauf
         var gradientShader = SKShader.CreateLinearGradient(
            new SKPoint(bounds.Left, bounds.Top),
            new SKPoint(bounds.Right, bounds.Bottom),
            new[] { SKColors.Orange, SKColors.Gold },
            null,
            SKShaderTileMode.Clamp
         );

         using var paint = new SKPaint {
            Shader = gradientShader,
            IsAntialias = true
         };

         canvas.DrawText(Text, x, y, font, paint);
      }

      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;
   }
}
