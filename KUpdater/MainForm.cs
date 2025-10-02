using KUpdater.Core;
using KUpdater.Interop;
using KUpdater.Scripting;
using KUpdater.UI;

namespace KUpdater {
   public partial class MainForm : Form {
      public static MainForm? Instance { get; private set; }

      private bool _isDragging = false;
      private Point _dragStart;

      private bool _isResizing = false;
      private Point _resizeStartCursor;
      private Size _resizeStartSize;
      private readonly int _resizeHitSize = 40;

      private readonly UIElementManager _uiElementManager;
      private readonly MainFormTheme _mainFormTheme;
      private readonly UIRenderer _uiRenderer;
      private readonly UpdaterConfig _config;
      private readonly Updater _updater;

      public MainForm() {
         Instance = this;

         _config = new LuaConfig<UpdaterConfig>("config.lua", "UpdaterConfig").Load();
         _updater = new Updater(new HttpUpdateSource(), _config.Url, AppDomain.CurrentDomain.BaseDirectory);

         _uiElementManager = new();
         _mainFormTheme = new(this, _uiElementManager, _updater, _config.Language);
         _uiRenderer = new(this, _uiElementManager, _mainFormTheme);

         InitializeComponent();
         FormBorderStyle = FormBorderStyle.None;
         StartPosition = FormStartPosition.CenterScreen;
         DoubleBuffered = true;

      }

      protected override CreateParams CreateParams {
         get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
         }
      }

      protected override void OnFormClosed(FormClosedEventArgs e) {
         _uiRenderer.Dispose();
         _mainFormTheme.Dispose();
         _uiElementManager.DisposeAndClearAll();
         Instance = null;
         base.OnFormClosed(e);
      }

      protected override async void OnShown(EventArgs e) {
         base.OnShown(e);
         _uiRenderer.RequestRender();

         _updater.StatusChanged += msg => {
            _mainFormTheme._lastStatus = msg;
            _uiRenderer.RequestRender();
         };

         _updater.ProgressChanged += val => {
            _mainFormTheme._lastProgress = val / 100.0;
            _uiRenderer.RequestRender();
         };

         await _updater.RunUpdateAsync();
      }


      protected override void OnResize(EventArgs e) {
         base.OnResize(e);
         _uiRenderer.RequestRender();
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

         // Let UIElementManager handle hover state for all controls
         if (_uiElementManager.MouseMove(e.Location))
            _uiRenderer.RequestRender();
      }

      protected override void OnMouseDown(MouseEventArgs e) {
         if (e.Button != MouseButtons.Left)
            return;

         // Erst an UIElementManager weitergeben
         bool handled = _uiElementManager.MouseDown(e.Location);
         if (handled) {
            _uiRenderer.RequestRender();
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

         // Pass to UIElementManager so controls can handle clicks
         if (_uiElementManager.MouseUp(e.Location))
            _uiRenderer.RequestRender();
      }

      protected override void OnMouseWheel(MouseEventArgs e) {
         bool handled = _uiElementManager.MouseWheel(e.Delta, e.Location);
         if (handled)
            _uiRenderer.RequestRender();
      }

   }
}
