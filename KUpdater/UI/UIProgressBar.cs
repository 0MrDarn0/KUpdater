using SkiaSharp;

namespace KUpdater.UI {
   public class UIProgressBar : IUIElement {
      public string Id { get; }
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();

      public bool Visible { get; set; } = true;
      public float Progress { get; set; } = 0f; // 0.0 - 1.0

      public SKColor FillColor { get; set; } = SKColors.Goldenrod;
      public SKColor BorderColor { get; set; } = SKColors.Black;
      public SKColor BackgroundColor { get; set; } = SKColors.Transparent;

      public UIProgressBar(string id, Func<Rectangle> boundsFunc) {
         Id = id;
         _boundsFunc = boundsFunc;
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;
         var rect = Bounds;
         using var brush = new SolidBrush(Color.FromArgb(FillColor.Alpha, FillColor.Red, FillColor.Green, FillColor.Blue));
         g.FillRectangle(brush, rect.X, rect.Y, rect.Width * Progress, rect.Height);
         g.DrawRectangle(Pens.White, rect);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible)
            return;
         var rect = Bounds;

         // Hintergrund
         if (BackgroundColor.Alpha > 0) {
            using var bgPaint = new SKPaint { Color = BackgroundColor, IsAntialias = true };
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, bgPaint);
         }

         // Fortschritt
         float barWidth = rect.Width * Math.Clamp(Progress, 0f, 1f);
         using (var fillPaint = new SKPaint { Color = FillColor, IsAntialias = true })
            canvas.DrawRect(rect.X, rect.Y, barWidth, rect.Height, fillPaint);

         // Rahmen
         using (var borderPaint = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2 })
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, borderPaint);
      }

      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;
   }
}
