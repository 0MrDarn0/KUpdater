using SkiaSharp;

namespace KUpdater.UI {
   public interface IUIElement {
      string Id { get; }
      protected static string Resource(string fileName) => Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);
      public bool Visible { get; set; }
      Rectangle Bounds { get; }
      void Draw(Graphics g);
      void Draw(SKCanvas canvas);
      bool OnMouseMove(Point p);
      bool OnMouseDown(Point p);
      bool OnMouseUp(Point p);
   }
}
