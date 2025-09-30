using SkiaSharp;

namespace KUpdater.UI {
   public class UIProgressBar : IUIElement {
      public string Id { get; }
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();

      public bool Visible { get; set; } = true;
      private float _progress = 0f;
      public float Progress {
         get => _progress;
         set => _progress = Math.Clamp(value, 0f, 1f);
      }

      // Farben
      private SKColor _fillColor = SKColors.Goldenrod;
      private SKColor _borderColor = SKColors.Black;
      private SKColor _backgroundColor = SKColors.Transparent;

      public SKColor FillColor {
         get => _fillColor;
         set { _fillColor = value; _fillPaint.Color = value; }
      }
      public SKColor BorderColor {
         get => _borderColor;
         set { _borderColor = value; _borderPaint.Color = value; }
      }
      public SKColor BackgroundColor {
         get => _backgroundColor;
         set { _backgroundColor = value; _bgPaint.Color = value; }
      }

      // 🧩 Skia Paints cachen
      private readonly SKPaint _fillPaint;
      private readonly SKPaint _borderPaint;
      private readonly SKPaint _bgPaint;

      public UIProgressBar(string id, Func<Rectangle> boundsFunc) {
         Id = id;
         _boundsFunc = boundsFunc;

         _fillPaint = new SKPaint { Color = _fillColor, IsAntialias = true };
         _borderPaint = new SKPaint { Color = _borderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
         _bgPaint = new SKPaint { Color = _backgroundColor, IsAntialias = true };
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
         if (_backgroundColor.Alpha > 0)
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _bgPaint);

         // Fortschritt
         float barWidth = rect.Width * Progress;
         canvas.DrawRect(rect.X, rect.Y, barWidth, rect.Height, _fillPaint);

         // Rahmen
         canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _borderPaint);
      }

      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;

      public void Dispose() {
         _fillPaint.Dispose();
         _borderPaint.Dispose();
         _bgPaint.Dispose();
      }
   }
}
