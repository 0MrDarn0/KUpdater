using System.Drawing.Drawing2D;

namespace KUpdater.UI {
   public static class Renderer {
      static Renderer() { }
      private static string Resource(string fileName) => Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);

      private class TextEntry {
         public string Text { get; init; } = string.Empty;
         public Font Font { get; init; } = SystemFonts.DefaultFont;
         public Point Position { get; init; }
         public Color Color { get; init; } = Color.White;
         public TextFormatFlags Flags { get; init; } = TextFormatFlags.Default;
      }
      private static readonly List<TextEntry> _texts = new();

      public static void AddText(string text, Font font, Point position, Color color, TextFormatFlags flags = TextFormatFlags.Default) {
         _texts.Add(new TextEntry { Text = text, Font = font, Position = position, Color = color, Flags = flags });
      }

      public static void DrawAllTexts(Graphics g) {
         foreach (var entry in _texts) {
            TextRenderer.DrawText(g, entry.Text, entry.Font, entry.Position, entry.Color, entry.Flags);
         }
      }

      public static void ClearTexts() => _texts.Clear();

      public static void DrawTitle(Graphics g, Size size) {
         Scripting.Theme theme = Scripting.LuaManager.GetParsedTheme();
         TextRenderer.DrawText(g, theme.Title, theme.TitleFont, theme.TitlePosition, theme.FontColor);
      }

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

      public static void DrawButton(Graphics g, Rectangle rect, string text, Font font, bool isHover, bool isPressed) {
         Color baseColor = isPressed ? Color.DarkRed : isHover ? Color.OrangeRed : Color.Red;
         using Brush brush = new SolidBrush(baseColor);
         g.FillRoundedRectangle(brush, rect, 6);
         TextRenderer.DrawText(g, text, font, rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      public static void DrawButtonWithImage(Graphics g, Rectangle rect, string baseName, string text, Font font, bool isHover, bool isPressed) {
         string state = isPressed ? "click" : isHover ? "hover" : "normal";
         using var img = Image.FromFile(Resource($"{baseName}_{state}.png"));
         g.DrawImage(img, rect);
         TextRenderer.DrawText(g, text, font, rect, Color.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      private static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle bounds, int radius) {
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
