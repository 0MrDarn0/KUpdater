using SkiaSharp;

namespace KUpdater.UI {
   public class UIButton : IUIElement {
      public string Id { get; }
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();
      public string Text { get; set; }
      public Font Font { get; set; }
      public Color Color { get; set; }
      public string ThemeKey { get; set; } // e.g. "btn_exit", "btn_default"
      public Action? OnClick { get; set; }
      public bool Visible { get; set; } = true;
      public bool IsHovered { get; private set; }
      public bool IsPressed { get; private set; }

      public UIButton(string id, Func<Rectangle> boundsFunc, string text, Font font, Color color, string themeKey, Action? onClick) {
         Id = id;
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         Color = color;
         ThemeKey = themeKey;
         OnClick = onClick;
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;

         string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
         using var img = Image.FromFile(IUIElement.Resource($"{ThemeKey}_{state}.png"));

         g.DrawImage(img, Bounds);
         TextRenderer.DrawText(g, Text, Font, Bounds, Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible)
            return;

         string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
         using var img = SKBitmap.Decode(IUIElement.Resource($"{ThemeKey}_{state}.png"));

         var bounds = Bounds;
         var destRect = new SKRect(bounds.X, bounds.Y, bounds.Right, bounds.Bottom);
         canvas.DrawBitmap(img, destRect);

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
         var x = bounds.X + bounds.Width / 2;
         var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2 - metrics.Descent * 0.3f;


         canvas.DrawText(Text, x, y, SKTextAlign.Center, font, paint);
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
   }
}
