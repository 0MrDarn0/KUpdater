using KUpdater.Settings;
using KUpdater.UI;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KUpdater
{
   public partial class MainForm : Form
   {
      public static MainForm? Instance { get; private set; }
      public string WindowTitle => _title;
      public KUpdaterSettings Settings => _settings;

      private readonly string _title = "kUpdater";
      private readonly KUpdaterSettings _settings;

      private bool _dragging = false;
      private Point _dragStart;
      private bool _resizing = false;
      private Point _resizeStartCursor;
      private Size _resizeStartSize;
      private const int _resizeHitSize = 40;
      private readonly Font _buttonFont = new("Segoe UI", 10, FontStyle.Bold);
      private readonly List<ButtonRegion> _buttons;

      public static class Paths
      {
         public static readonly string ResourceDir = Path.Combine(AppContext.BaseDirectory, "kUpdater");
      }

      public MainForm()
      {
         Instance = this;
         InitializeComponent();
         _settings = SettingsManager.Load();
         _title = _settings.Title;

         this.FormBorderStyle = FormBorderStyle.None;
         this.StartPosition = FormStartPosition.CenterScreen;
         _buttons =
         [
             new ButtonRegion(() =>
                    new Rectangle(
                        this.Width - 35,
                        16,
                        18, 18),
                        "X",
                        "btn_exit",
                        () => this.Close()),
            ];
      }
      protected override CreateParams CreateParams
      {
         get
         {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED
            return cp;
         }
      }
      protected override void OnShown(EventArgs e)
      {
         base.OnShown(e);
         SafeRedraw();
      }
      protected override void OnMouseMove(MouseEventArgs e)
      {
         if (_resizing)
         {
            Point delta = new Point(Cursor.Position.X - _resizeStartCursor.X, Cursor.Position.Y - _resizeStartCursor.Y);
            int newWidth = Math.Max(300, _resizeStartSize.Width + delta.X);
            int newHeight = Math.Max(200, _resizeStartSize.Height + delta.Y);
            this.Size = new Size(newWidth, newHeight);
            SafeRedraw();
            return;
         }

         if (_dragging)
         {
            Point newLocation = new Point(this.Left + e.X - _dragStart.X, this.Top + e.Y - _dragStart.Y);
            this.Location = newLocation;
            return;
         }

         this.Cursor = new Rectangle(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize)
             .Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;

         bool needsRedraw = false;
         foreach (var btn in _buttons)
         {
            bool prev = btn.IsHovered;
            btn.IsHovered = btn.Bounds.Contains(e.Location);
            if (prev != btn.IsHovered) needsRedraw = true;
         }

         if (needsRedraw)
            SafeRedraw();
      }
      protected override void OnMouseDown(MouseEventArgs e)
      {
         if (e.Button == MouseButtons.Left)
         {
            foreach (var btn in _buttons)
            {
               if (btn.Bounds.Contains(e.Location))
               {
                  btn.IsPressed = true;
                  SafeRedraw();
                  return;
               }
            }

            var resizeRect = new Rectangle(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize);
            if (resizeRect.Contains(e.Location))
            {
               _resizing = true;
               _resizeStartCursor = Cursor.Position;
               _resizeStartSize = this.Size;
            }
            else
            {
               _dragging = true;
               _dragStart = e.Location;
            }
         }
      }
      protected override void OnMouseUp(MouseEventArgs e)
      {
         _dragging = false;
         _resizing = false;

         foreach (var btn in _buttons)
         {
            if (btn.IsPressed && btn.Bounds.Contains(e.Location))
            {
               btn.IsPressed = false;
               btn.OnClick?.Invoke();
               return;
            }
            btn.IsPressed = false;
         }

         SafeRedraw();
      }
      protected override void OnMouseClick(MouseEventArgs e)
      {
         var closeRect = new Rectangle(this.Width - 35, 15, 20, 20); // Position des Close-Buttons
         if (closeRect.Contains(e.Location))
         {
            this.Close();
         }
      }
      private void SafeRedraw()
      {
         if (this.IsDisposed || !this.IsHandleCreated) return;

         Bitmap bmp = new(
             this.Width,
             this.Height,
             PixelFormat.Format32bppArgb);

         using (Graphics g = Graphics.FromImage(bmp))
         {
            g.Clear(Color.Transparent);

            UI.Renderer.DrawBackground(g, this.Size);
            UI.Renderer.DrawTitle(g, this.Size);

            foreach (ButtonRegion button in _buttons)
               button.Draw(g, _buttonFont);
         }
         SetBitmap(bmp, 255);
      }
      private void SetBitmap(Bitmap bitmap, byte opacity)
      {
         var screenDc = NativeMethods.GetDC(IntPtr.Zero);
         var memDc = NativeMethods.CreateCompatibleDC(screenDc);
         var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
         var oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

         Size size = new(bitmap.Width, bitmap.Height);
         Point source = new(0, 0);
         Point topPos = new(this.Left, this.Top);

         var blend = new NativeMethods.BLENDFUNCTION
         {
            BlendOp = NativeMethods.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = opacity,
            AlphaFormat = NativeMethods.AC_SRC_ALPHA
         };

         var success = NativeMethods.UpdateLayeredWindow(
              this.Handle,
              screenDc,
              ref topPos,
              ref size,
              memDc,
              ref source,
              0,
              ref blend,
              NativeMethods.ULW_ALPHA);

         if (!success)
         {
            var err = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
         }

         _ = NativeMethods.SelectObject(memDc, oldBitmap);
         _ = NativeMethods.DeleteObject(hBitmap);
         _ = NativeMethods.DeleteDC(memDc);
         _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
      }
   }
}
