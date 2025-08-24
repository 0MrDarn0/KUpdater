using KUpdater.Scripting;
using KUpdater.UI;
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

         _uiManager = new();
         _luaManager = new(_uiManager);
         _luaManager.Init("theme_loader.lua");
         _luaManager.LoadTheme("default");

         this.FormBorderStyle = FormBorderStyle.None;
         this.StartPosition = FormStartPosition.CenterScreen;
         this.DoubleBuffered = true;

         _uiManager.Add(new UIButton(
            () => new Rectangle(Width - 35, 16, 18, 18),
            "X",
            new Font("Segoe UI", 10, FontStyle.Bold),
            "btn_exit",
            () => Close()));

         _uiManager.Add(new UIButton(
             () => new Rectangle(Width - 150, Height - 70, 97, 22),
             "Start",
             new Font("Segoe UI", 9, FontStyle.Bold),
             "btn_default",
             StartGame));


         _uiManager.Add(new UIButton(
             () => new Rectangle(Width - 255, Height - 70, 97, 22),
             "Settings",
             new Font("Segoe UI", 9, FontStyle.Bold),
             "btn_default",
             OpenSettings));
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

      protected override void OnMouseMove(MouseEventArgs e) {
         if (_isResizing) {
            Point delta = new(
               Cursor.Position.X - _resizeStartCursor.X,
               Cursor.Position.Y - _resizeStartCursor.Y
               );

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

            this.Size = new Size(newWidth, newHeight);
            _luaManager.ReInitTheme();
            SafeRedraw();
            return;
         }

         if (_isDragging) {
            Point newLocation = new(this.Left + e.X - _dragStart.X, this.Top + e.Y - _dragStart.Y);
            this.Location = newLocation;
            return;
         }

         this.Cursor = new Rectangle(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize)
             .Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;

         // Let UIManager handle hover state for all controls
         if (_uiManager.MouseMove(e.Location))
            SafeRedraw();
      }

      protected override void OnMouseDown(MouseEventArgs e) {
         if (e.Button == MouseButtons.Left) {
            // Resize hotspot
            Rectangle resizeRect = new(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize);
            if (resizeRect.Contains(e.Location)) {
               _isResizing = true;
               _resizeStartCursor = Cursor.Position;
               _resizeStartSize = this.Size;
               return;
            }

            // Start dragging if not on a control
            _isDragging = true;
            _dragStart = e.Location;

            // Also pass to UIManager so controls can react
            if (_uiManager.MouseDown(e.Location))
               SafeRedraw();
         }
      }
      protected override void OnMouseUp(MouseEventArgs e) {
         _isDragging = false;
         _isResizing = false;

         // Pass to UIManager so controls can handle clicks
         if (_uiManager.MouseUp(e.Location))
            SafeRedraw();
      }

      internal void SafeRedraw() {
         if (this.IsDisposed || !this.IsHandleCreated)
            return;

         Bitmap bmp = new(
             this.Width,
             this.Height,
             PixelFormat.Format32bppArgb);

         using (Graphics g = Graphics.FromImage(bmp)) {
            g.Clear(Color.Transparent);

            UI.Renderer.DrawBackground(g, this.Size);
            _uiManager.Draw(g);
         }
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

      private static void StartGame() {
         try {
            string exePath = Path.Combine(Application.StartupPath, "engine.exe");

            if (!File.Exists(exePath))
               throw new FileNotFoundException("The game executable 'engine.exe' was not found.", exePath);

            Process.Start(new ProcessStartInfo {
               FileName = exePath,
               Arguments = "/load /config debug",
               UseShellExecute = false
            });

            Environment.Exit(0); // Immediately close the launcher
         }
         catch (Exception ex) {
            MessageBox.Show(
                $"Unable to launch the game.\n\nDetails: {ex.Message}",
                "Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
         }
      }

      private static void OpenSettings() {
         try {
            string exePath = Path.Combine(Application.StartupPath, "engine.exe");

            if (!File.Exists(exePath))
               throw new FileNotFoundException("The game executable 'engine.exe' was not found.", exePath);

            Process.Start(new ProcessStartInfo {
               FileName = exePath,
               Arguments = "/setup",
               UseShellExecute = false
            });
         }
         catch (Exception ex) {
            MessageBox.Show(
                $"Unable to open the settings.\n\nDetails: {ex.Message}",
                "Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
         }
      }
   }
}
