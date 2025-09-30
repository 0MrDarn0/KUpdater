using SkiaSharp;

namespace KUpdater.UI {
   public class UIElementManager {
      private readonly List<IUIElement> _elements = [];
      public void Add(IUIElement element) => _elements.Add(element);

      public void ClearAll() {
         foreach (var el in _elements.OfType<IDisposable>())
            el.Dispose();
         _elements.Clear();
      }

      public void ClearLabels() {
         foreach (var label in _elements.OfType<UILabel>().OfType<IDisposable>())
            label.Dispose();
         _elements.RemoveAll(e => e is UILabel);
      }

      public void ClearButtons() {
         foreach (var button in _elements.OfType<UIButton>().OfType<IDisposable>())
            button.Dispose();
         _elements.RemoveAll(e => e is UIButton);
      }

      public T? FindById<T>(string id) where T : class, IUIElement
         => _elements.OfType<T>().FirstOrDefault(e => e.Id == id);

      public void UpdateLabel(string id, string newText) {
         var label = FindById<UILabel>(id);
         label?.Text = newText;
      }

      public void UpdateProgressBar(string id, double value) {
         var bar = FindById<UIProgressBar>(id);
         bar?.Progress = (float)Math.Clamp(value, 0.0, 1.0);
      }

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