// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Core.UI;
using KUpdater.Interop;
using KUpdater.Scripting;
using KUpdater.Scripting.Theme;
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
        private readonly MainTheme _theme;
        private readonly UIRenderer _uiRenderer;
        private readonly UpdaterConfig _config;

        private readonly IEventDispatcher _dispatcher;
        private readonly UpdaterPipelineRunner _runner;
        private readonly UIState _uiState = new();


        public MainForm() {
            Instance = this;

            _config = new LuaConfig<UpdaterConfig>("config.lua", "UpdaterConfig").Load();

            _dispatcher = new EventDispatcher();
            var source = new HttpUpdateSource();
            _runner = new UpdaterPipelineRunner(_dispatcher, source, _config.Url, AppDomain.CurrentDomain.BaseDirectory);

            _uiElementManager = new();
            _theme = new(this, _uiElementManager, _uiState, _config.Language);
            _uiRenderer = new(this, _uiElementManager, _theme);

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
            _theme.Dispose();
            _uiElementManager.DisposeAndClearAll();
            Instance = null;
            base.OnFormClosed(e);
        }

        protected override async void OnShown(EventArgs e) {
            base.OnShown(e);
            _uiRenderer.RequestRender();

            // Events abonnieren
            _dispatcher.Subscribe<StatusEvent>(ev => {
                _uiState.SetStatus(ev.Text);
                _uiRenderer.RequestRender();
            });

            _dispatcher.Subscribe<ProgressEvent>(ev => {
                _uiState.SetProgress(ev.Percent);
                _uiRenderer.RequestRender();
            });

            _dispatcher.Subscribe<ChangelogEvent>(ev => {
                _uiState.SetChangelog(ev.Text);
                _uiRenderer.RequestRender();
            });

            // Pipeline starten
            await _runner.RunAsync(AppDomain.CurrentDomain.BaseDirectory);
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
