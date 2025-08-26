using SkiaSharp;

namespace KUpdater.UI {
   public class UILabel : IUIElement {
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();
      public string Text { get; set; }
      public Font Font { get; set; }
      public Color Color { get; set; }
      public TextFormatFlags Flags { get; set; }
      public bool Visible { get; set; } = true;

      public UILabel(Func<Rectangle> boundsFunc, string text, Font font, Color color, TextFormatFlags flags = TextFormatFlags.Default) {
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         Color = color;
         Flags = flags;
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;
         TextRenderer.DrawText(g, Text, Font, Bounds.Location, Color, Flags);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible)
            return;

         var bounds = Bounds;

         SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
         SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

         var typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));

         using var font = new SKFont {
            Typeface = typeface,
            Size = Font.Size * 1.33f
         };

         using var paint = new SKPaint {
            Color = Color.ToSKColor(),
            IsAntialias = true
         };

         var metrics = font.Metrics;
         var x = bounds.X;
         var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;

         canvas.DrawText(Text, x, y, SKTextAlign.Left, font, paint);
      }


      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;
   }
}
