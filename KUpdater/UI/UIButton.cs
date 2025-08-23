namespace KUpdater.UI {
   public class UIButton : IUIElement {
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();
      public string Text { get; set; }
      public Font Font { get; set; }
      public string ThemeKey { get; set; } // e.g. "btn_exit", "btn_default"
      public Action? OnClick { get; set; }
      public bool Visible { get; set; } = true;
      public bool IsHovered { get; private set; }
      public bool IsPressed { get; private set; }

      public UIButton(Func<Rectangle> boundsFunc, string text, Font font, string themeKey, Action? onClick) {
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         ThemeKey = themeKey;
         OnClick = onClick;
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;

         string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
         using var img = Image.FromFile(IUIElement.Resource($"{ThemeKey}_{state}.png"));

         g.DrawImage(img, Bounds);
         TextRenderer.DrawText(g, Text, Font, Bounds, Color.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
