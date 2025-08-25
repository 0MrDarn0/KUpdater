using KUpdater.Scripting;
using KUpdater.UI;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KUpdater {
   public partial class MainForm : Form {
      public static MainForm? Instance { get; private set; }

      private bool _isDragging = false;
      private Point _dragStart;

      private bool _isResizing = false;
      private Point _resizeStartCursor;
      private Size _resizeStartSize;
      private readonly int _resizeHitSize = 40;

      private readonly UIManager _uiManager;
      private readonly LuaManager _luaManager;

      public MainForm() {
         Instance = this;
         InitializeComponent();

         FormBorderStyle = FormBorderStyle.None;
         StartPosition = FormStartPosition.CenterScreen;
         DoubleBuffered = true;
         BackColor = Color.Lime;
         TransparencyKey = Color.Empty;

         _uiManager = new();
         _luaManager = new(_uiManager);
         _luaManager.Init("theme_loader.lua");
         _luaManager.LoadTheme("default");
      }

      protected override CreateParams CreateParams {
         get {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED
            return cp;
         }
      }

      protected override void OnFormClosed(FormClosedEventArgs e) {
         Instance = null;
         base.OnFormClosed(e);
      }

      protected override void OnShown(EventArgs e) {
         base.OnShown(e);
         SafeRedraw();
      }

      protected override void OnResize(EventArgs e) {
         base.OnResize(e);
         _luaManager?.ReInitTheme();
         SafeRedraw();
      }

      protected override void OnMouseMove(MouseEventArgs e) {
         if (_isResizing) {
            Point delta = new(
               Cursor.Position.X - _resizeStartCursor.X,
               Cursor.Position.Y - _resizeStartCursor.Y);

            // Bildschirm-Arbeitsbereich holen (ohne Taskleiste)
            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;

            // Dynamische Maximalwerte
            int maxWidth = workArea.Width;
            int maxHeight = workArea.Height;

            // Neue Größe berechnen
            int newWidth = _resizeStartSize.Width + delta.X;
            int newHeight = _resizeStartSize.Height + delta.Y;

            // Mindest- und Höchstwerte anwenden
            newWidth = Math.Max(450, Math.Min(newWidth, maxWidth));
            newHeight = Math.Max(300, Math.Min(newHeight, maxHeight));

            // Nur Größe setzen – Rest macht OnResize
            this.Size = new Size(newWidth, newHeight);
            return;
         }

         if (_isDragging) {
            Point newLocation = new(this.Left + e.X - _dragStart.X, this.Top + e.Y - _dragStart.Y);
            this.Location = newLocation;
            return;
         }

         this.Cursor = new Rectangle(
             this.Width - _resizeHitSize,
             this.Height - _resizeHitSize,
             _resizeHitSize,
             _resizeHitSize
         ).Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;

         // Let UIManager handle hover state for all controls
         if (_uiManager.MouseMove(e.Location))
            SafeRedraw();
      }

      protected override void OnMouseDown(MouseEventArgs e) {
         if (e.Button != MouseButtons.Left)
            return;

         // Erst an UIManager weitergeben
         bool handled = _uiManager.MouseDown(e.Location);
         if (handled) {
            SafeRedraw();
            return; // Wenn ein Element reagiert, nicht weiterziehen!
         }

         // Resize hotspot
         Rectangle resizeRect = new(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize);
         if (resizeRect.Contains(e.Location)) {
            _isResizing = true;
            _resizeStartCursor = Cursor.Position;
            _resizeStartSize = this.Size;
            return;
         }

         // Fensterbewegung starten
         _isDragging = true;
         _dragStart = e.Location;
      }

      protected override void OnMouseUp(MouseEventArgs e) {
         _isDragging = false;
         _isResizing = false;

         // Pass to UIManager so controls can handle clicks
         if (_uiManager.MouseUp(e.Location))
            SafeRedraw();
      }

      internal void SafeRedraw() {
         if (IsDisposed || !IsHandleCreated)
            return;

         using var skBmp = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
         using var surface = SKSurface.Create(skBmp.Info, skBmp.GetPixels(), skBmp.RowBytes);
         var canvas = surface.Canvas;

         // Hintergrund + UI zeichnen
         UI.Renderer.DrawBackground(canvas, new Size(Width, Height));
         _uiManager.Draw(canvas);

         // Skia → GDI Bitmap kopieren
         using var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
         var bmpData = bmp.LockBits(new Rectangle(0, 0, Width, Height),
                               ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
         Marshal.Copy(skBmp.Bytes, 0, bmpData.Scan0, skBmp.Bytes.Length);
         bmp.UnlockBits(bmpData);

         SetBitmap(bmp, 255);
      }

      private void SetBitmap(Bitmap bitmap, byte opacity) {
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

         if (!success) {
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
