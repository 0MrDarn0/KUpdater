using System.Drawing;
using System.Windows.Forms;

namespace KUpdater.UI
{
    public static class Renderer
    {
        public static void DrawBackground(Graphics g, Size size)
        {
            int width = size.Width;
            int height = size.Height;

            g.Clear(Color.Transparent);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // Rahmen-Grafiken laden
            var topLeft = Image.FromFile("Resources/border_top_left.png");
            var topCenter = Image.FromFile("Resources/border_top_center.png");
            var topRight = Image.FromFile("Resources/border_top_right.png");
            var rightCenter = Image.FromFile("Resources/border_right_center.png");
            var bottomRight = Image.FromFile("Resources/border_bottom_right.png");
            var bottomCenter = Image.FromFile("Resources/border_bottom_center.png");
            var bottomLeft = Image.FromFile("Resources/border_bottom_left.png");
            var leftCenter = Image.FromFile("Resources/border_left_center.png");

            // Ecken
            g.DrawImage(topLeft, 0, 0, topLeft.Width, topLeft.Height);
            g.DrawImage(topRight, width - topRight.Width, 0, topRight.Width, topRight.Height);
            g.DrawImage(bottomLeft, 0, height - bottomLeft.Height, bottomLeft.Width, bottomLeft.Height);
            g.DrawImage(bottomRight, width - bottomRight.Width + 1, height - bottomRight.Height, bottomRight.Width, bottomRight.Height);

            // Kanten (stretch)
            g.DrawImage(topCenter, new Rectangle(topLeft.Width, 0, width - topLeft.Width - topRight.Width + 10, topCenter.Height));
            g.DrawImage(bottomCenter, new Rectangle(bottomLeft.Width, height - bottomCenter.Height, width - bottomLeft.Width - bottomRight.Width + 21, bottomCenter.Height));
            g.DrawImage(leftCenter, new Rectangle(0, topLeft.Height, leftCenter.Width, height - topLeft.Height - bottomLeft.Height + 5));
            g.DrawImage(rightCenter, new Rectangle(width - rightCenter.Width, topRight.Height, rightCenter.Width, height - topRight.Height - bottomRight.Height + 5));

            // Innenfläche
            g.FillRectangle(Brushes.Black, new Rectangle(leftCenter.Width - 5, topCenter.Height - 5, width - leftCenter.Width * 2 + 12, height - topCenter.Height - bottomCenter.Height + 9));
        }

        public static void DrawButton(Graphics g, Rectangle rect, string text, Font font, bool isHover, bool isPressed)
        {
            Color baseColor = isPressed ? Color.DarkRed :
                              isHover ? Color.OrangeRed : Color.Red;

            using Brush brush = new SolidBrush(baseColor);
            g.FillRoundedRectangle(brush, rect, 6); // Erweiterung nötig

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
            string imagePath = $"Resources/{baseName}_{state}.png";

            using var img = Image.FromFile(imagePath);
            g.DrawImage(img, rect);

            // Optional: Text auf Bild zeichnen
            TextRenderer.DrawText(
                g,
                text,
                font,
                rect,
                Color.White,
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
