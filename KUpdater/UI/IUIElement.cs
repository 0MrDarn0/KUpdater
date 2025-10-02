using SkiaSharp;

namespace KUpdater.UI {
   public interface IUIElement : IDisposable {
      string Id { get; }
      public bool Visible { get; set; }
      Rectangle Bounds { get; }
      void Draw(Graphics g);
      void Draw(SKCanvas canvas);
      bool OnMouseMove(Point p);
      bool OnMouseDown(Point p);
      bool OnMouseUp(Point p);

      // 🆕 Scroll-Event
      bool OnMouseWheel(int delta, Point p);
   }
}
