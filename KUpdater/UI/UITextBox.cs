using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI {
   public class UITextBox : IUIElement {
      public string Id { get; }
      private readonly Func<Rectangle> _boundsFunc;
      public Rectangle Bounds => _boundsFunc();

      public string Text { get; set; }
      public Font Font { get; }
      public Color ForeColor { get; set; }
      public Color BackColor { get; set; }
      public bool Visible { get; set; } = true;
      public bool ReadOnly { get; set; }
      public bool Multiline { get; set; }

      private readonly bool _ownsFont;

      // Skia Ressourcen
      private SKTypeface? _typeface;
      private SKFont? _skFont;
      private SKPaint? _textPaint;
      private SKPaint? _bgPaint;

      private int _scrollOffset = 0;   // in Pixeln
      private int _lineHeight;


      public UITextBox(string id, Func<Rectangle> boundsFunc, string text, Font font,
                       Color foreColor, Color backColor,
                       bool multiline = true, bool readOnly = false, bool ownsFont = true) {
         Id = id;
         _boundsFunc = boundsFunc;
         Text = text;
         Font = font;
         ForeColor = foreColor;
         BackColor = backColor;
         Multiline = multiline;
         ReadOnly = readOnly;
         _ownsFont = ownsFont;
         InitSkiaResources();
      }

      public UITextBox(string id, Table bounds, string text, Font font, Color foreColor, Color backColor,
                       bool multiline = true, bool readOnly = false, bool ownsFont = true)
          : this(id, () => new Rectangle(
              (int)(bounds.Get("x").CastToNumber() ?? 0),
              (int)(bounds.Get("y").CastToNumber() ?? 0),
              (int)(bounds.Get("width").CastToNumber() ?? 0),
              (int)(bounds.Get("height").CastToNumber() ?? 0)
          ), text, font, foreColor, backColor, multiline, readOnly, ownsFont) { }

      private void InitSkiaResources() {
         SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
         SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

         _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
         _skFont = new SKFont(_typeface, Font.Size * 1.33f);
         _textPaint = new SKPaint { Color = ForeColor.ToSKColor(), IsAntialias = true };
         _bgPaint = new SKPaint { Color = BackColor.ToSKColor(), IsAntialias = true, Style = SKPaintStyle.Fill };
      }

      public void Draw(Graphics g) {
         if (!Visible)
            return;

         // Optional: Fallback GDI+ Rendering
         g.FillRectangle(new SolidBrush(BackColor), Bounds);
         TextRenderer.DrawText(g, Text, Font, Bounds, ForeColor,
             TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
      }

      public void Draw(SKCanvas canvas) {
         if (!Visible || _skFont == null || _textPaint == null || _bgPaint == null)
            return;

         var rect = Bounds;

         // Hintergrund
         canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _bgPaint);

         // Clipping aktivieren
         canvas.Save();
         canvas.ClipRect(new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom));

         // Text zeichnen mit ScrollOffset
         _lineHeight = (int)(_skFont.Metrics.CapHeight * 1.4f);
         float x = rect.X + 4;
         float y = rect.Y + _lineHeight - _scrollOffset; // 👈 Offset anwenden

         if (Multiline) {
            foreach (var line in Text.Split(["\r\n", "\n"], StringSplitOptions.None)) {
               // Nur zeichnen, wenn Zeile im sichtbaren Bereich liegt
               if (y + _lineHeight >= rect.Top && y <= rect.Bottom) {
                  canvas.DrawText(line, x, y, SKTextAlign.Left, _skFont, _textPaint);
               }

               y += _lineHeight;
               if (y > rect.Bottom)
                  break; // Nicht über den unteren Rand hinaus zeichnen
            }
         } else {
            canvas.DrawText(Text, x, y, SKTextAlign.Left, _skFont, _textPaint);
         }

         // Clipping wiederherstellen
         canvas.Restore();
      }

      public bool OnMouseMove(Point p) => false;
      public bool OnMouseDown(Point p) => false;
      public bool OnMouseUp(Point p) => false;

      public bool OnMouseWheel(int delta, Point p) {
         if (!Bounds.Contains(p))
            return false;

         int scrollStep = _lineHeight * 3;
         _scrollOffset -= Math.Sign(delta) * scrollStep;

         int maxScroll = Math.Max(0, (int)(GetTotalTextHeight() - Bounds.Height));
         if (_scrollOffset < 0)
            _scrollOffset = 0;
         if (_scrollOffset > maxScroll)
            _scrollOffset = maxScroll;

         return true; // handled
      }


      private float GetTotalTextHeight() {
         int lines = Text.Split(["\r\n", "\n"], StringSplitOptions.None).Length;
         return lines * _lineHeight;
      }

      public void Dispose() {
         if (_ownsFont)
            Font.Dispose();

         _textPaint?.Dispose();
         _bgPaint?.Dispose();
         _skFont?.Dispose();
         _typeface?.Dispose();
      }
   }
}
