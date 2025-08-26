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

      private readonly UIManager _uiManager;
      private readonly MainFormTheme _mainFormTheme;
      private readonly UIRenderer _uiRenderer;

      public MainForm() {
         Instance = this;
         _uiManager = new();
         _mainFormTheme = new("main_form", _uiManager);
         _uiRenderer = new(this, _uiManager, _mainFormTheme);

         InitializeComponent();

         FormBorderStyle = FormBorderStyle.None;
         StartPosition = FormStartPosition.CenterScreen;
         DoubleBuffered = true;
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
         _uiRenderer.Redraw();
      }

      protected override void OnResize(EventArgs e) {
         base.OnResize(e);
         _mainFormTheme?.ReInitTheme();
         _uiRenderer.Redraw();
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
            _uiRenderer.Redraw();
      }

      protected override void OnMouseDown(MouseEventArgs e) {
         if (e.Button != MouseButtons.Left)
            return;

         // Erst an UIManager weitergeben
         bool handled = _uiManager.MouseDown(e.Location);
         if (handled) {
            _uiRenderer.Redraw();
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
            _uiRenderer.Redraw();
      }
   }
}
