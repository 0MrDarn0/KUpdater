using System.Drawing.Drawing2D;

namespace KUpdater.UI {
   public static class Renderer {
      static Renderer() { }
      public static void DrawBackground(Graphics g, Size size) {
         var bg = Scripting.LuaManager.GetBackground();

         int width = size.Width;
         int height = size.Height;
         g.Clear(Color.Transparent);
         g.InterpolationMode = InterpolationMode.HighQualityBicubic;

         // Ecken
         g.DrawImage(bg.TopLeft, 0, 0, bg.TopLeft.Width, bg.TopLeft.Height);
         g.DrawImage(bg.TopRight, width - bg.TopRight.Width, 0, bg.TopRight.Width, bg.TopRight.Height);
         g.DrawImage(bg.BottomLeft, 0, height - bg.BottomLeft.Height, bg.BottomLeft.Width, bg.BottomLeft.Height);
         g.DrawImage(bg.BottomRight, width - bg.BottomRight.Width, height - bg.BottomRight.Height, bg.BottomRight.Width, bg.BottomRight.Height);

         // Kanten (stretch)
         int top_width_off = 7;
         int bottom_width_off = 21;
         int left_height_off = 5;
         int right_height_off = 5;

         g.DrawImage(bg.TopCenter, new Rectangle(bg.TopLeft.Width, 0, width - bg.TopLeft.Width - bg.TopRight.Width + top_width_off, bg.TopCenter.Height));
         g.DrawImage(bg.BottomCenter, new Rectangle(bg.BottomLeft.Width, height - bg.BottomCenter.Height, width - bg.BottomLeft.Width - bg.BottomRight.Width + bottom_width_off, bg.BottomCenter.Height));
         g.DrawImage(bg.LeftCenter, new Rectangle(0, bg.TopLeft.Height, bg.LeftCenter.Width, height - bg.TopLeft.Height - bg.BottomLeft.Height + left_height_off));
         g.DrawImage(bg.RightCenter, new Rectangle(width - bg.RightCenter.Width, bg.TopRight.Height, bg.RightCenter.Width, height - bg.TopRight.Height - bg.BottomRight.Height + right_height_off));

         // Innenfläche
         int fill_pos_off = 5;
         int fill_width_off = 12;
         int fill_height_off = 12;

         g.FillRectangle(new SolidBrush(bg.FillColor),
             new Rectangle(bg.LeftCenter.Width - fill_pos_off, bg.TopCenter.Height - fill_pos_off,
             width - bg.LeftCenter.Width * 2 + fill_width_off,
             height - bg.TopCenter.Height - bg.BottomCenter.Height + fill_height_off));
      }
   }
}
