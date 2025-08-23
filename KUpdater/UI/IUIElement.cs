namespace KUpdater.UI {
   public interface IUIElement {
      Rectangle Bounds { get; }
      void Draw(Graphics g);
      bool OnMouseMove(Point p);
      bool OnMouseDown(Point p);
      bool OnMouseUp(Point p);
   }
}
