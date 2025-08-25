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

         using var font = new SKFont {
            Typeface = SKTypeface.FromFamilyName(Font.Name),
            Size = Font.Size * 1.33f // pt → px grob
         };

         using var paint = new SKPaint {
            Color = new SKColor(Color.R, Color.G, Color.B, Color.A),
            IsAntialias = true
         };

         var x = bounds.X;
         var y = bounds.Y + font.Size;

         canvas.DrawText(Text, x, y, SKTextAlign.Left, font, paint);
      }


      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;
   }
}
