using SkiaSharp;
using System.Diagnostics;

namespace KUpdater.UI {
   public class UIElementManager {
      private readonly List<IUIElement> _elements = new();
      public void Add(IUIElement element) => _elements.Add(element);
      public void ClearAll() => _elements.Clear();
      public void ClearLabels() => _elements.RemoveAll(e => e is UILabel);
      public void ClearButtons() => _elements.RemoveAll(e => e is UIButton);

      public T? FindById<T>(string id) where T : class, IUIElement
         => _elements.OfType<T>().FirstOrDefault(e => e.Id == id);

      public void UpdateLabel(string id, string newText) {
         var label = FindById<UILabel>(id);
         //Debug.WriteLine($"UpdateLabel {id} -> {newText}, found={label != null}");
         label?.Text = newText;
      }

      public void UpdateProgressBar(string id, double value) {
         var bar = FindById<UIProgressBar>(id);
         //Debug.WriteLine($"UpdateProgress {id} -> {value}, found={bar != null}");
         bar?.Progress = (float)Math.Clamp(value, 0.0, 1.0);
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