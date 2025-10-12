// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Core.UI;
using KUpdater.Interop;
using KUpdater.Scripting;
using KUpdater.Scripting.Theme;
using KUpdater.UI;
using KUpdater.Utility;
using SniffKit.Core;
using SniffKit.IO;
using SniffKit.UI;

namespace KUpdater {
    public partial class MainForm : Form {
        public static MainForm? Instance { get; private set; }

        private bool _isDragging = false;
        private Point _dragStart;

        private bool _isResizing = false;
        private Point _resizeStartCursor;
        private Size _resizeStartSize;
        private readonly int _resizeHitSize = 40;

        private readonly MainTheme _theme;
        private readonly Renderer _uiRenderer;
        private readonly IEventManager _eventManager;
        private readonly UpdaterPipelineRunner _runner;
        private readonly ControlManager _uiElementManager;
        private readonly UpdaterConfig _config;
        private readonly TrayIcon? _trayIcon;
        private readonly UIState _uiState = new();
        private readonly Logger _logger;
        private readonly IResourceProvider _resourceProvider;

        public MainForm() {
            Instance = this;
            _logger = new Logger("MainLogger")
                .SetLogDirectory(Paths.AppFolder)
                .EnableDebugOutput(true)
                .WriteToFile(true, LogType.Info);

            _config = new LuaConfig<UpdaterConfig>("config.lua", "UpdaterConfig").Load();

            _eventManager = new EventManager();
            var source = new HttpUpdateSource();
            _runner = new UpdaterPipelineRunner(_eventManager, source, _config.Url, AppDomain.CurrentDomain.BaseDirectory);

            _resourceProvider = new FileResourceProvider(Paths.ResFolder, strongCacheCapacity: 16);
            _uiElementManager = new();
            _theme = new(this, _uiElementManager, _uiState, _config.Language, _resourceProvider);
            _uiRenderer = new(this, _uiElementManager, _theme);

            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            _trayIcon = new TrayIcon()
                .Name("kUpdater")
                .Icon(Paths.Resource("Default/app.ico"))
                .StatusIcons(status => status
                    .Item("default", Paths.Resource("Default/app.ico"))
                )
                .Menu(menu => menu
                    .Item("Settings", (s, e) => { _logger.Info("Settings clicked"); })
                    .Separator()
                    .Exit((s, e) => Application.Exit()));
        }

        protected override CreateParams CreateParams {
            get {
                var cp = base.CreateParams;
                cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
                return cp;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            _trayIcon?.Dispose();
            _uiRenderer.Dispose();
            _theme.Dispose();
            _uiElementManager.DisposeAndClearAll();
            _resourceProvider?.Dispose();
            Instance = null;
            base.OnFormClosed(e);
        }

        protected override async void OnShown(EventArgs e) {
            base.OnShown(e);
            _uiRenderer.RequestRender();

            // Events abonnieren
            _eventManager.Register<StatusEvent>(ev => {
                _uiState.SetStatus(ev.Text);
                _uiRenderer.RequestRender();
            });

            _eventManager.Register<ProgressEvent>(ev => {
                _uiState.SetProgress(ev.Percent);
                _uiRenderer.RequestRender();
            });

            _eventManager.Register<ChangelogEvent>(ev => {
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

            // Let ControlManager handle hover state for all controls
            if (_uiElementManager.MouseMove(e.Location))
                _uiRenderer.RequestRender();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            if (e.Button != MouseButtons.Left)
                return;

            // Erst an ControlManager weitergeben
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

            // Pass to ControlManager so controls can handle clicks
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
