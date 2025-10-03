// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Reflection;
using KUpdater.Core;
using KUpdater.Extensions;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;


namespace KUpdater.Scripting {

    public class MainFormTheme : Lua, ITheme {
        private readonly Form _form;
        private readonly UIElementManager _uiElementManager;
        private readonly Updater _updater;
        private string? _currentTheme;
        private ThemeBackground? _cachedBackground;
        private ThemeLayout? _cachedLayout;
        private readonly ResourceManager _resources;
        private bool _disposed;

        // State
        public string _lastStatus = "Status: Waiting...";
        public double _lastProgress = 0.0;
        public string _lastChangeLog = "Changelog ....";

        // 🔗 Bindings
        private readonly Action<string> _setStatusText;
        private readonly Action<double> _setProgress;
        private readonly Action<string> _setChangeLogText;

        public MainFormTheme(Form form, UIElementManager uiElementManager, Updater updater, string language) : base("theme_loader.lua") {
            _form = form;
            _uiElementManager = uiElementManager;
            _updater = updater;
            _resources = new ResourceManager();

            // Init bindings
            _setStatusText = UIBindings.BindLabelText(_uiElementManager, UIBindings.Ids.UpdateStatusLabel);
            _setProgress = UIBindings.BindProgress(_uiElementManager, UIBindings.Ids.UpdateProgressBar);
            _setChangeLogText = UIBindings.BindTextBoxText(_uiElementManager, UIBindings.Ids.ChangeLogTextBox);

            LoadLanguage(language);
            RegisterGlobals();
            LoadTheme("main_form");
        }

        public void ApplyLastState() {
            _setStatusText(_lastStatus);
            _setProgress(_lastProgress);
            _setChangeLogText(_lastChangeLog);
        }

        protected override void RegisterGlobals() {
            base.RegisterGlobals();

            ExposeToLua("uiElement", _uiElementManager);
            ExposeToLua<Font>();
            ExposeToLua<Color>();

            ExposeToLua("MakeColor", new {
                FromHex = (Func<string, Color>)MakeColor.FromHex,
                ToHex = (Func<Color, string>)MakeColor.ToHex,
                FromRgb = (Func<int, int, int, Color>)MakeColor.FromRgb,
                FromRgba = (Func<int, int, int, int, Color>)MakeColor.FromRgba
            });


            SetGlobal(LuaKeys.Theme.ThemeDir, Paths.LuaThemes.Replace("\\", "/"));
            SetGlobal(LuaKeys.UI.GetWindowSize, (Func<DynValue>)(() => {
                return DynValue.NewTuple(
                   DynValue.NewNumber(_form?.Width ?? 0),
                   DynValue.NewNumber(_form?.Height ?? 0));
            }));


            // 🔥 Lua-Callbacks direkt mit Bindings verbinden
            SetGlobal("update_status", (Action<string>)(_setStatusText));
            SetGlobal("update_download_progress", (Action<double>)(_setProgress));

            // 🔗 Generische Updates für dynamische Elemente
            SetGlobal("update_label", UIBindings.UpdateLabel(_uiElementManager));
            SetGlobal("update_progress", UIBindings.UpdateProgress(_uiElementManager));


            SetGlobal("open_website", (Action<string>)((url) => {
                try {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open website: {ex.Message}");
                }
            }));

            SetGlobal(LuaKeys.Actions.StartGame, (Action)(() => GameLauncher.StartGame()));
            SetGlobal(LuaKeys.Actions.OpenSettings, (Action)(() => GameLauncher.OpenSettings()));
            SetGlobal(LuaKeys.Actions.ApplicationExit, (Action)(() => Application.Exit()));


            // 🔥 Automatische Registrierung aller IUIElement-Klassen
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IUIElement).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)) {
                var method = typeof(Lua).GetMethod(nameof(ExposeToLua))!;
                var generic = method.MakeGenericMethod(type);
                generic.Invoke(this, [null, null]);
            }
        }

        public void LoadTheme(string themeName) {
            ObjectDisposedException.ThrowIf(_script == null, this);

            _currentTheme = themeName;
            // Lua-Funktion "load_theme" aufrufen
            Invoke(LuaKeys.Theme.LoadTheme, themeName);

            // Init-Funktion nur aufrufen, wenn vorhanden
            var initFunc = new LuaValue<Closure>(GetTheme().Get("init"));
            if (initFunc.IsValid)
                Invoke(initFunc.Raw);

        }


        public void ReInitTheme() {
            if (!string.IsNullOrEmpty(_currentTheme)) {
                _uiElementManager.DisposeAndClearAll();
                LoadTheme(_currentTheme);
                ApplyLastState();
            }
        }

        public Table GetTheme() => Invoke(LuaKeys.Theme.GetTheme).Table;

        public Table GetThemeTable(string key) {
            var table = new LuaValue<Table>(GetTheme().Get(key));
            if (!table.IsValid)
                Debug.WriteLine($"[Lua] Theme key '{key}' is not a table.");
            return table.Value ?? new Table(_script);
        }

        public ThemeBackground GetBackground() => _cachedBackground ??= BuildBackground();
        public ThemeLayout GetLayout() => _cachedLayout ??= BuildLayout();

        public ThemeBackground BuildBackground() {
            var bg = new ThemeTable(GetThemeTable("background"), _script);
            return new ThemeBackground {
                TopLeft = _resources.GetSkiaBitmap(bg.GetString("top_left")),
                TopCenter = _resources.GetSkiaBitmap(bg.GetString("top_center")),
                TopRight = _resources.GetSkiaBitmap(bg.GetString("top_right")),
                RightCenter = _resources.GetSkiaBitmap(bg.GetString("right_center")),
                BottomRight = _resources.GetSkiaBitmap(bg.GetString("bottom_right")),
                BottomCenter = _resources.GetSkiaBitmap(bg.GetString("bottom_center")),
                BottomLeft = _resources.GetSkiaBitmap(bg.GetString("bottom_left")),
                LeftCenter = _resources.GetSkiaBitmap(bg.GetString("left_center")),
                FillColor = bg.GetColor("fill_color", Color.Black)
            };
        }


        public ThemeLayout BuildLayout() {
            var layout = new ThemeTable(GetThemeTable("layout"), _script);
            return new ThemeLayout {
                TopWidthOffset = layout.GetInt("top_width_offset"),
                BottomWidthOffset = layout.GetInt("bottom_width_offset"),
                LeftHeightOffset = layout.GetInt("left_height_offset"),
                RightHeightOffset = layout.GetInt("right_height_offset"),
                FillPosOffset = layout.GetInt("fill_pos_offset"),
                FillWidthOffset = layout.GetInt("fill_width_offset"),
                FillHeightOffset = layout.GetInt("fill_height_offset")
            };
        }

        public void LoadLanguage(string langCode) {
            var langPath   = Paths.LuaLanguage(langCode);
            var defaultLangPath= Paths.LuaDefaultLanguage;

            if (!File.Exists(langPath))
                throw new FileNotFoundException($"Language file not found: {langPath}");

            // Lade aktuelle Sprache
            var langTable = _script.DoString(File.ReadAllText(langPath)).Table;

            // Lade Fallback (Englisch)
            var fallbackTable = _script.DoString(File.ReadAllText(defaultLangPath)).Table;

            SetGlobal("L", langTable);
            SetGlobal("L_Fallback", fallbackTable);

            // Registriere Lookup-Funktion mit Fallback
            SetGlobal("T", (Func<string, string>)(key => {
                string? Lookup(LuaValue<DynValue> table) {
                    var node = table.Raw;
                    foreach (var part in key.Split('.')) {
                        if (!node.IsTable())
                            return null;
                        node = node.Table.Get(part);
                    }
                    return node.AsString();
                }

                return Lookup(GetGlobal<DynValue>("L"))
                    ?? Lookup(GetGlobal<DynValue>("L_Fallback"))
                    ?? $"[MISSING:{key}]";
            }));

            Localization.Initialize(_script);
        }

        protected override void Dispose(bool disposing) {
            if (_disposed)
                return;

            if (disposing) {
                // Theme-Ressourcen freigeben
                _cachedBackground = null;
                _cachedLayout = null;
                _resources.Dispose();

                // Lua-Basis freigeben
                base.Dispose(disposing);
            }

            _disposed = true;
        }
    }
}
