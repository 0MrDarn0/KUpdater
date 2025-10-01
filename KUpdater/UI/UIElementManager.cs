using SkiaSharp;

namespace KUpdater.UI {
   public class UIElementManager {
      private readonly List<IUIElement> _elements = [];
      public void Add(IUIElement element) => _elements.Add(element);

      public void DisposeAndClearAll() {
         int count = _elements.Count;
         foreach (var el in _elements)
            el.Dispose();
         _elements.Clear();
         System.Diagnostics.Debug.WriteLine($"[UIElementManager] Disposed {count} elements (DisposeAndClearAll).");
      }

      public void DisposeAndClear<T>() where T : class, IUIElement {
         int count = _elements.Count(e => e is T);
         foreach (var el in _elements.OfType<T>())
            el.Dispose();
         _elements.RemoveAll(e => e is T);
         System.Diagnostics.Debug.WriteLine($"[UIElementManager] Disposed {count} {typeof(T).Name}(s).");
      }

      public T? FindById<T>(string id) where T : class, IUIElement
         => _elements.OfType<T>().FirstOrDefault(e => e.Id == id);

      public void Update<T>(string id, Action<T> updater) where T : class, IUIElement {
         var el = FindById<T>(id);
         if (el != null)
            updater(el);
      }

      public void Draw(Graphics g) {
         foreach (var el in _elements)
            if (el.Visible)
               el.Draw(g);
      }

      public void Draw(SKCanvas canvas) {
         foreach (var el in _elements)
            if (el.Visible)
               el.Draw(canvas);
      }

      public bool MouseMove(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements.ToList())
            if (el.Visible && el.OnMouseMove(p))
               needsRedraw = true;
         return needsRedraw;
      }

      public bool MouseDown(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements.ToList())
            if (el.Visible && el.OnMouseDown(p))
               needsRedraw = true;
         return needsRedraw;
      }

      public bool MouseUp(Point p) {
         bool needsRedraw = false;
         foreach (var el in _elements.ToList())
            if (el.Visible && el.OnMouseUp(p))
               needsRedraw = true;
         return needsRedraw;
      }
   }
}