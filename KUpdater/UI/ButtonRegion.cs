namespace KUpdater.UI
{
   public class ButtonRegion
   {
      private readonly Func<Rectangle> _boundsFunc;
      public string Text { get; }
      public string ImageKey { get; }
      public Action OnClick { get; }

      public bool IsHovered { get; set; }
      public bool IsPressed { get; set; }

      public Rectangle Bounds => _boundsFunc();

      public ButtonRegion(Func<Rectangle> boundsFunc, string text, string imageKey, Action onClick)
      {
         this._boundsFunc = boundsFunc;
         Text = text;
         ImageKey = imageKey;
         OnClick = onClick;
      }

      public void Draw(Graphics g, Font font)
      {
         UI.Renderer.DrawButtonWithImage(g, Bounds, ImageKey, Text, font, IsHovered, IsPressed);
      }

   }
}
