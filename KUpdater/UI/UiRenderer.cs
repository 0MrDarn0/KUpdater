namespace KUpdater.UI
{
   public static class Renderer
   {
      /*
       ҳ̸Ҳ̸ҳ
      kalonline:
      칼온라인
      Schwert oder Gewalt
      웃
      유
       */
      private static string Resource(string fileName) => Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);

      private static readonly Font _windowTitleFont;
      private static readonly Color _windowTitleColor;
      private static readonly Point _windowTitlePosition;

      // Border Images
      private static readonly Image _topLeftBorder = AssetLoader.RequireImage("border_top_left.png");
      private static readonly Image _topCenterBorder = AssetLoader.RequireImage("border_top_center.png");
      private static readonly Image _topRightBorder = AssetLoader.RequireImage("border_top_right.png");
      private static readonly Image _rightCenterBorder = AssetLoader.RequireImage("border_right_center.png");
      private static readonly Image _bottomRightBorder = AssetLoader.RequireImage("border_bottom_right.png");
      private static readonly Image _bottomCenterBorder = AssetLoader.RequireImage("border_bottom_center.png");
      private static readonly Image _bottomLeftBorder = AssetLoader.RequireImage("border_bottom_left.png");
      private static readonly Image _leftCenterBorder = AssetLoader.RequireImage("border_left_center.png");

      static Renderer()
      {
         var settings = KUpdater.Settings.SettingsManager.Load();

         // FontStyle
         if (!Enum.TryParse(settings.FontStyle, true, out FontStyle style))
            style = FontStyle.Regular;
         try
         {
            _windowTitleFont = new Font(settings.FontFamily, settings.FontSize, style);
         }
         catch
         {
            _windowTitleFont = new Font("Segoe UI", 40f, FontStyle.Regular); // Fallback
         }


         // Color
         try
         {
            _windowTitleColor = ColorTranslator.FromHtml(settings.TitleColor);
         }
         catch
         {
            _windowTitleColor = Color.DarkOrange;
         }

         // Position
         _windowTitlePosition = new Point(settings.TitlePosition.X, settings.TitlePosition.Y);
      }
      private static void DebugDraw(Graphics g, Rectangle rect)
      {
         g.DrawRectangle(new(color: Color.Magenta, 1), rect);
      }

      public static void DrawTitle(Graphics g, Size size)
      {
         Theme theme = LuaManager.GetParsedTheme();
         TextRenderer.DrawText(g, theme.Title, theme.TitleFont, theme.TitlePosition, theme.FontColor);

         //string title = MainForm.Instance?.WindowTitle ?? "kUpdater";
         //TextRenderer.DrawText(g, title, _windowTitleFont, _windowTitlePosition, _windowTitleColor);
         /*
          * Rectangle titleArea = new(0, -20, size.Width, 55);
          * DebugDraw(g, titleArea);
          * TextRenderer.DrawText(g, _windowTitle, _windowTitleFont, titleArea, Color.DarkOrange, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
          */
      }

      public static void DrawBackground(Graphics g, Size size)
      {
         int width = size.Width;
         int height = size.Height;
         g.Clear(Color.Transparent);
         g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
         // Ecken
         g.DrawImage(_topLeftBorder, 0, 0, _topLeftBorder.Width, _topLeftBorder.Height);
         g.DrawImage(_topRightBorder, width - _topRightBorder.Width, 0, _topRightBorder.Width, _topRightBorder.Height);
         g.DrawImage(_bottomLeftBorder, 0, height - _bottomLeftBorder.Height, _bottomLeftBorder.Width, _bottomLeftBorder.Height);
         g.DrawImage(_bottomRightBorder, width - _bottomRightBorder.Width, height - _bottomRightBorder.Height, _bottomRightBorder.Width, _bottomRightBorder.Height);
         // Kanten (stretch)
         int top_width_off = 7;
         int bottom_width_off = 21;
         int left_height_off = 5;
         int right_height_off = 5;
         g.DrawImage(_topCenterBorder, new Rectangle(_topLeftBorder.Width, 0, width - _topLeftBorder.Width - _topRightBorder.Width + top_width_off, _topCenterBorder.Height));
         g.DrawImage(_bottomCenterBorder, new Rectangle(_bottomLeftBorder.Width, height - _bottomCenterBorder.Height, width - _bottomLeftBorder.Width - _bottomRightBorder.Width + bottom_width_off, _bottomCenterBorder.Height));
         g.DrawImage(_leftCenterBorder, new Rectangle(0, _topLeftBorder.Height, _leftCenterBorder.Width, height - _topLeftBorder.Height - _bottomLeftBorder.Height + left_height_off));
         g.DrawImage(_rightCenterBorder, new Rectangle(width - _rightCenterBorder.Width, _topRightBorder.Height, _rightCenterBorder.Width, height - _topRightBorder.Height - _bottomRightBorder.Height + right_height_off));

         // Innenfläche
         int fill_pos_off = 5;
         int fill_width_off = 12;
         int fill_height_off = 12;
         g.FillRectangle(Brushes.Black, new Rectangle(_leftCenterBorder.Width - fill_pos_off, _topCenterBorder.Height - fill_pos_off, width - _leftCenterBorder.Width * 2 + fill_width_off, height - _topCenterBorder.Height - _bottomCenterBorder.Height + fill_height_off));
      }

      public static void DrawButton(Graphics g, Rectangle rect, string text, Font font, bool isHover, bool isPressed)
      {
         Color baseColor = isPressed ? Color.DarkRed : isHover ? Color.OrangeRed : Color.Red;
         using Brush brush = new SolidBrush(baseColor);
         g.FillRoundedRectangle(brush, rect, 6);

         TextRenderer.DrawText(
             g,
             text,
             font,
             rect,
             Color.White,
             TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      public static void DrawButtonWithImage(Graphics g, Rectangle rect, string baseName, string text, Font font, bool isHover, bool isPressed)
      {
         string state = isPressed ? "click" : isHover ? "hover" : "normal";
         using var img = Image.FromFile(Resource($"{baseName}_{state}.png"));

         //draw the button
         g.DrawImage(img, rect);

         // draw the text ontop
         TextRenderer.DrawText(
             g,
             text,
             font,
             rect,
             Color.Gold,
             TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      private static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle bounds, int radius)
      {
         using var path = new System.Drawing.Drawing2D.GraphicsPath();
         path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
         path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
         path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
         path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
         path.CloseFigure();
         g.FillPath(brush, path);
      }

   }
}
