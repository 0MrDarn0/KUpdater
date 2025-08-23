namespace KUpdater.UI {
   public class ButtonRegion {
      private readonly Func<Rectangle> _boundsFunc;
      public string Text { get; }
      public Font Font { get; }
      public string ImageKey { get; }
      public Action OnClick { get; }
      public bool IsHovered { get; set; }
      public bool IsPressed { get; set; }

      public Rectangle Bounds => _boundsFunc();

      public ButtonRegion(Func<Rectangle> boundsFunc, string text, Font font, string imageKey, Action onClick) {
         this._boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         ImageKey = imageKey;
         OnClick = onClick;
      }

      public void Draw(Graphics g) {
         UI.Renderer.DrawButtonWithImage(g, Bounds, ImageKey, Text, Font, IsHovered, IsPressed);
         //UI.Renderer.DrawButtonWithLua(g, Bounds, Text, IsHovered, IsPressed);
      }

   }
}
