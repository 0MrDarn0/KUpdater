using System.Drawing.Drawing2D;

namespace KUpdater.UI {
   public static class Renderer {
      static Renderer() { }
      private static string Resource(string fileName) => Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);

      public static event Action? RequestRedraw;

      public static int EinblendGeschwindigkeit = 2;
      public static int AusblendGeschwindigkeit = 2;
      public static double SichtbarSekunden = 5.0;
      public static int FramesPerSecond = 60;

      private static float _animAlpha;
      private static float _textY;
      private static float _targetY;
      private static bool _isAnimatingIn;
      private static bool _isAnimatingOut;
      private static bool _isAnimationPaused = false;
      private static System.Windows.Forms.Timer? _animation_timer;
      private static DateTime _visibleSince;

      private class TextEntry {
         public string Text { get; init; } = string.Empty;
         public Font Font { get; init; } = SystemFonts.DefaultFont;
         public Point Position { get; init; }
         public Color Color { get; init; } = Color.White;
         public TextFormatFlags Flags { get; init; } = TextFormatFlags.Default;
      }
      private static readonly List<TextEntry> _texts = new();

      public static void AddText(string text, Font font, Point position, Color color, TextFormatFlags flags = TextFormatFlags.Default) {
         _texts.Add(new TextEntry {
            Text = text,
            Font = font,
            Position = position,
            Color = color,
            Flags = flags
         });
      }

      public static void DrawAllTexts(Graphics g) {
         foreach (var entry in _texts) {
            TextRenderer.DrawText(
                g,
                entry.Text,
                entry.Font,
                entry.Position,
                entry.Color,
                entry.Flags
            );
         }
      }

      public static void ClearTexts() => _texts.Clear();

      private static void DebugDraw(Graphics g, Rectangle rect) {
         g.DrawRectangle(new(color: Color.Magenta, 1), rect);
      }

      public static void InitTextAnimation(Size formSize) {
         _animAlpha = 0f;
         _textY = -20; // Start oberhalb des sichtbaren Bereichs
         _targetY = 50; // Zielposition (10px vom oberen Rand)
      }

      public static void StartTextAnimation() {
         if (_animation_timer == null) {
            _animation_timer = new System.Windows.Forms.Timer { Interval = 1000 / FramesPerSecond };
            _animation_timer.Tick += UpdateAnimation;
         }

         _isAnimatingIn = true;
         _isAnimatingOut = false;
         _animAlpha = 0f;
         _animation_timer.Start();
      }

      private static void UpdateAnimation(object? sender, EventArgs e) {
         if (_isAnimatingIn) {
            _animAlpha = Math.Min(1f, _animAlpha + 0.05f);
            _textY += EinblendGeschwindigkeit;

            if (_animAlpha >= 1f && _textY >= _targetY) {
               _animAlpha = 1f;
               _textY = _targetY;
               _isAnimatingIn = false;
               _visibleSince = DateTime.Now;
            }
         } else if (!_isAnimatingOut && _animAlpha >= 1f) {
            if ((DateTime.Now - _visibleSince).TotalSeconds >= SichtbarSekunden) {
               _isAnimatingOut = true;
            }
         } else if (_isAnimatingOut) {
            _animAlpha = Math.Max(0f, _animAlpha - 0.05f);
            _textY -= AusblendGeschwindigkeit;

            if (_animAlpha <= 0f) {
               _isAnimatingOut = false;
               _animation_timer?.Stop();
            }
         }
         RequestRedraw?.Invoke();
      }

      public static void PauseAnimation(bool pause) {
         if (pause && !_isAnimationPaused) {
            _animation_timer?.Stop();
            _isAnimationPaused = true;
         } else if (!pause && _isAnimationPaused) {
            _animation_timer?.Start();
            _isAnimationPaused = false;
         }
      }

      public static void DrawAnimatedCopyright(Graphics g) {
         string text = "kUpdater © 2025 Darn";
         using Font font = new("Segoe UI", 12f, FontStyle.Bold);

         int alpha = (int)(_animAlpha * 255);
         using SolidBrush brush = new(Color.FromArgb(alpha, 118, 92, 61));

         g.DrawString(text, font, brush, new PointF(30, _textY));
      }

      public static void DrawTitle(Graphics g, Size size) {
         Theme theme = LuaManager.GetParsedTheme();
         TextRenderer.DrawText(g, theme.Title, theme.TitleFont, theme.TitlePosition, theme.FontColor);
      }

      public static void DrawBackground(Graphics g, Size size) {
         var bg = LuaManager.GetBackground();

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

         TextRenderer.DrawText(
             g,
             text,
             font,
             rect,
             Color.White,
             TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      }

      public static void DrawButtonWithImage(Graphics g, Rectangle rect, string baseName, string text, Font font, bool isHover, bool isPressed) {
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

      public static void DrawButtonWithLua(Graphics g, Rectangle rect, string text, bool isHover, bool isPressed) {
         Theme theme = LuaManager.GetParsedTheme();

         Color color = isPressed ? theme.Button.Pressed :
                       isHover ? theme.Button.Hover : theme.Button.Normal;

         using Brush brush = new SolidBrush(color);
         g.FillRoundedRectangle(brush, rect, 6);

         TextRenderer.DrawText(
             g,
             text,
             theme.Button.Font,
             rect,
             theme.Button.FontColor,
             TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
