namespace KUpdater.Scripting {

   public interface ITheme {
      ThemeBackground GetBackground();
      ThemeLayout GetLayout();
   }

   public class ThemeBackground {
      public Image TopLeft { get; set; } = new Bitmap(1, 1);
      public Image TopCenter { get; set; } = new Bitmap(1, 1);
      public Image TopRight { get; set; } = new Bitmap(1, 1);
      public Image RightCenter { get; set; } = new Bitmap(1, 1);
      public Image BottomRight { get; set; } = new Bitmap(1, 1);
      public Image BottomCenter { get; set; } = new Bitmap(1, 1);
      public Image BottomLeft { get; set; } = new Bitmap(1, 1);
      public Image LeftCenter { get; set; } = new Bitmap(1, 1);
      public Color FillColor { get; set; } = Color.Black;
   }

   public class ThemeLayout {
      public int TopWidthOffset { get; set; }
      public int BottomWidthOffset { get; set; }
      public int LeftHeightOffset { get; set; }
      public int RightHeightOffset { get; set; }
      public int FillPosOffset { get; set; }
      public int FillWidthOffset { get; set; }
      public int FillHeightOffset { get; set; }
   }
}
