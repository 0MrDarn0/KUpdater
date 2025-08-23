namespace KUpdater.UI {
   public class UIManager {
      private readonly List<IUIElement> _elements = new();
      public void Add(IUIElement element) => _elements.Add(element);
      public void ClearLabels() {
         _elements.RemoveAll(e => e is UILabel);
      }

      public void Draw(Graphics g) {
         foreach (var el in _elements)
            if (el.Visible)
               el.Draw(g);
      }

      public bool MouseMove(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements)
            if (el.Visible && el.OnMouseMove(p))
               needsRedraw = true;
         return needsRedraw;
      }

      public bool MouseDown(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements)
            if (el.Visible && el.OnMouseDown(p))
               needsRedraw = true;
         return needsRedraw;
      }

      public bool MouseUp(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements)
            if (el.Visible && el.OnMouseUp(p))
               needsRedraw = true;
         return needsRedraw;
      }
   }
}