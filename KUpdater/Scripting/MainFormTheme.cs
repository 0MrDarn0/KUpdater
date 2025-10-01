using System.Diagnostics;
using System.Reflection;
using KUpdater.UI;
using MoonSharp.Interpreter;
using SkiaSharp;


namespace KUpdater.Scripting {

   public class MainFormTheme : Lua, ITheme, IDisposable {
      private readonly Form _form;
      private readonly UIElementManager _uiElementManager;
      private readonly Dictionary<string, SKBitmap> _imageCache = [];
      private string? _currentTheme;
      private ThemeBackground? _cachedBackground;
      private ThemeLayout? _cachedLayout;

      // State
      public string _lastStatus = "Status: Waiting...";
      public double _lastProgress = 0.0;

      // 🔗 Bindings
      private readonly Action<string> _setStatusText;
      private readonly Action<double> _setProgress;

      public MainFormTheme(Form form, UIElementManager uiElementManager) : base("theme_loader.lua") {
         _form = form;
         _uiElementManager = uiElementManager;

         // Init bindings
         _setStatusText = UIBindings.BindLabelText(_uiElementManager, UIBindings.Ids.UpdateStatusLabel);
         _setProgress = UIBindings.BindProgress(_uiElementManager, UIBindings.Ids.UpdateProgressBar);

         SetGlobal(LuaKeys.Theme.ThemeDir, Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes").Replace("\\", "/"));
         RegisterGlobals();
         LoadTheme("main_form");

      }

      public void ApplyLastState() {
         _setStatusText(_lastStatus);
         _setProgress(_lastProgress);
      }

      protected override void RegisterGlobals() {
         base.RegisterGlobals();

         ExposeToLua("uiElement", _uiElementManager);
         ExposeToLua<Font>();
         ExposeToLua<Color>();

         SetGlobal(LuaKeys.UI.GetWindowSize, (Func<DynValue>)(() => {
            return DynValue.NewTuple(
               DynValue.NewNumber(_form?.Width ?? 0),
               DynValue.NewNumber(_form?.Height ?? 0));
         }));


         SetGlobal(LuaKeys.UI.AddLabel,
            (Action<string, string, double, double, DynValue, string, double, string>)
            ((id, text, x, y, colorVal, fontName, fontSize, fontStyle) => {
               Color color = ToColor(colorVal, Color.White);
               if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                  style = FontStyle.Regular;
               Font font = new(fontName, (float)fontSize, style);
               _uiElementManager.Add(new UILabel(
                  id,
                  () => new Rectangle(
                     (int)(x < 0 ? _form.Width + x : x),
                     (int)(y < 0 ? _form.Height + y : y),
                     TextRenderer.MeasureText(text, font).Width,
                     TextRenderer.MeasureText(text, font).Height),
                  text, font, color));
            }));



         SetGlobal(LuaKeys.UI.AddButton,
             (Action<string, string, double, double, double, double, string, double, string, DynValue, string, DynValue>)
             ((id, text, x, y, width, height, fontName, fontSize, fontStyle, colorVal, imageKey, callback) => {
                Color color = ToColor(colorVal, Color.White);

                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;

                Font font = new(fontName, (float)fontSize, style);

                var button = new UIButton(
                   id,
                   () => new Rectangle(
                      (int)(x < 0 ? _form.Width + x : x),
                      (int)(y < 0 ? _form.Height + y : y),
                      (int)width, (int)height),
                   text, font, color, imageKey,
                   () => CallDynFunction(callback));
                _uiElementManager.Add(button);
             }));




         SetGlobal("add_progressbar",
             (Action<string, double, double, double, double>)
             ((id, x, y, width, height) => {
                var bar = new UIProgressBar(
                   id,
                   () => new Rectangle(
                      (int)(x < 0 ? _form.Width + x : x),
                      (int)(y < 0 ? _form.Height + y : y),
                      (int)(width < 0 ? _form.Width + width : width),
                      (int)height));
                _uiElementManager.Add(bar);
             }));


         // 🔥 Lua-Callbacks direkt mit Bindings verbinden
         SetGlobal("update_status", (Action<string>)(_setStatusText));
         SetGlobal("update_download_progress", (Action<double>)(_setProgress));

         // 🔗 Generische Updates für dynamische Elemente
         SetGlobal("update_label", UIBindings.UpdateLabel(_uiElementManager));
         SetGlobal("update_progress", UIBindings.UpdateProgress(_uiElementManager));

         SetGlobal("reinit_theme", (Action)(() => ReInitTheme()));


         SetGlobal("open_website", (Action<string>)((url) => {
            try {
               var psi = new ProcessStartInfo
            {
                  FileName = url,
                  UseShellExecute = true
               };
               Process.Start(psi);
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
            var method = typeof(Lua).GetMethod(nameof(ExposeToLua))!.MakeGenericMethod(type);
            method.Invoke(this, [null, null]);
         }
      }

      public void ClearImageCache() {
         foreach (var img in _imageCache.Values)
            img?.Dispose();
         _imageCache.Clear();
      }

      public void LoadTheme(string themeName) {
         if (_script == null)
            throw new ObjectDisposedException(nameof(MainFormTheme));

         _currentTheme = themeName;
         ClearImageCache();
         _cachedBackground = null;
         _cachedLayout = null;

         // Lua-Funktion "load_theme" aufrufen
         CallFunction(LuaKeys.Theme.LoadTheme, themeName);

         // Init-Funktion nur aufrufen, wenn vorhanden
         var theme = GetTheme();
         var initFunc = theme.Get("init");
         if (initFunc.Type == DataType.Function)
            CallDynFunction(initFunc);
      }


      public void ReInitTheme() {
         if (!string.IsNullOrEmpty(_currentTheme)) {
            _uiElementManager.DisposeAndClearAll();
            LoadTheme(_currentTheme);
            ApplyLastState();
         }
      }

      public Table GetTheme() => CallFunction(LuaKeys.Theme.GetTheme).Table;
      public Table GetThemeTable(string key) {
         var value = GetTheme().Get(key);
         if (value.Type != DataType.Table) {
            Debug.WriteLine($"[Lua] Theme key '{key}' is not a table.");
            return new Table(_script);
         }
         return value.Table;
      }
      public ThemeBackground GetBackground() => _cachedBackground ??= BuildBackground();
      public ThemeLayout GetLayout() => _cachedLayout ??= BuildLayout();

      public ThemeBackground BuildBackground() {
         var bg = GetThemeTable("background");
         return new ThemeBackground {
            TopLeft = LoadSkiaBitmap(bg, "top_left"),
            TopCenter = LoadSkiaBitmap(bg, "top_center"),
            TopRight = LoadSkiaBitmap(bg, "top_right"),
            RightCenter = LoadSkiaBitmap(bg, "right_center"),
            BottomRight = LoadSkiaBitmap(bg, "bottom_right"),
            BottomCenter = LoadSkiaBitmap(bg, "bottom_center"),
            BottomLeft = LoadSkiaBitmap(bg, "bottom_left"),
            LeftCenter = LoadSkiaBitmap(bg, "left_center"),
            FillColor = ToColor(bg.Get("fill_color"), Color.Black)
         };
      }

      public ThemeLayout BuildLayout() {
         var layout = GetThemeTable("layout");
         return new ThemeLayout {
            TopWidthOffset = (int)(layout.Get("top_width_offset").CastToNumber() ?? 0),
            BottomWidthOffset = (int)(layout.Get("bottom_width_offset").CastToNumber() ?? 0),
            LeftHeightOffset = (int)(layout.Get("left_height_offset").CastToNumber() ?? 0),
            RightHeightOffset = (int)(layout.Get("right_height_offset").CastToNumber() ?? 0),
            FillPosOffset = (int)(layout.Get("fill_pos_offset").CastToNumber() ?? 0),
            FillWidthOffset = (int)(layout.Get("fill_width_offset").CastToNumber() ?? 0),
            FillHeightOffset = (int)(layout.Get("fill_height_offset").CastToNumber() ?? 0)
         };
      }

      //private Image LoadImage(Table table, string key) {
      //   string file = table.Get(key).CastToString();
      //   if (string.IsNullOrWhiteSpace(file))
      //      return new Bitmap(1, 1);

      //   if (_imageCache.TryGetValue(file, out var cached))
      //      return cached;

      //   string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);
      //   if (!File.Exists(path))
      //      return new Bitmap(1, 1);

      //   using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
      //   var img = Image.FromStream(fs);
      //   _imageCache[file] = img;
      //   return img;
      //}

      private SKBitmap LoadSkiaBitmap(Table table, string key) {
         string file = table.Get(key).CastToString();
         if (string.IsNullOrWhiteSpace(file))
            return new SKBitmap(1, 1);

         if (_imageCache.TryGetValue(file, out var cached))
            return (SKBitmap)cached;

         string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);
         if (!File.Exists(path))
            return new SKBitmap(1, 1);

         using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
         using var img = Image.FromStream(fs);
         var skBmp = img.ToSKBitmap();
         _imageCache[file] = skBmp;
         return skBmp;
      }


      private Color ToColor(DynValue val, Color fallback) {
         try {
            if (val.Type == DataType.String) {
               return ColorTranslator.FromHtml(val.String);
            }

            if (val.UserData?.Object is Color c) {
               return c;
            }
         }
         catch {
            // Ignorieren und auf Fallback zurückfallen
         }

         return fallback;
      }



      public override void Dispose() {
         // eigene Ressourcen freigeben
         foreach (var bmp in _imageCache.Values) {
            bmp?.Dispose();
         }
         _imageCache.Clear();

         _cachedBackground = null;
         _cachedLayout = null;

         // Basisklasse aufräumen (Lua-Script + Globals)
         base.Dispose();
      }

   }
}
