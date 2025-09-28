using KUpdater.UI;
using MoonSharp.Interpreter;
using System.Diagnostics;
using System.Reflection;

namespace KUpdater.Scripting {

   public class MainFormTheme : Lua, ITheme {
      private readonly Form _form;
      private readonly UIElementManager _uiElementManager;
      private readonly Dictionary<string, Image> _imageCache = new();
      private string? _currentTheme;
      public string _lastStatus = "Status: Waiting...";
      public double _lastProgress = 0.0;

      public MainFormTheme(Form form, UIElementManager uiElementManager) : base("theme_loader.lua") {
         _form = form;
         _uiElementManager = uiElementManager;
         SetGlobal(LuaKeys.Theme.ThemeDir, Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes").Replace("\\", "/"));
         LoadTheme("main_form");

      }

      public void ApplyLastState() {
         _uiElementManager.UpdateLabel("lb_update_status", _lastStatus);
         _uiElementManager.UpdateProgressBar("pb_update_progress", _lastProgress);
      }

      protected override void RegisterGlobals() {
         base.RegisterGlobals();

         SetGlobal(LuaKeys.UI.GetWindowSize, (Func<DynValue>)(() => {
            return DynValue.NewTuple(
               DynValue.NewNumber(_form?.Width ?? 0),
               DynValue.NewNumber(_form?.Height ?? 0));
         }));


         SetGlobal(LuaKeys.UI.AddLabel,
             (Action<string, string, double, double, string, string, double, string>)
             ((id, text, x, y, colorHex, fontName, fontSize, fontStyle) =>
             {
                Color color = ColorTranslator.FromHtml(colorHex);
                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;
                Font font = new(fontName, (float)fontSize, style);

                _uiElementManager.Add(new UILabel(id,
                   () => new Rectangle(
                      (int)(x < 0 ? _form.Width + x : x), 
                      (int)(y < 0 ? y : y),                
                      TextRenderer.MeasureText(text, font).Width,
                      TextRenderer.MeasureText(text, font).Height),
                   text, font, color));
             }));



         SetGlobal(LuaKeys.UI.AddButton,
             (Action<string, string, double, double, double, double, string, double, string, string, string, DynValue>)
             ((id, text, x, y, width, height, fontName, fontSize, fontStyle, colorHex, imageKey, callback) =>
             {
                Color color = ColorTranslator.FromHtml(colorHex);
                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;
                Font font = new(fontName, (float)fontSize, style);

                var button = new UIButton(
                   id,
                   () => new Rectangle(
                      (int)(x < 0 ? _form.Width + x : x),
                      (int)(y < 0 ? _form.Height + y : y),
                      (int)width, (int)height),
                   text, font, color, imageKey, () => CallDynFunction(callback));
                _uiElementManager.Add(button);
             }));



         SetGlobal("add_progressbar",
             (Action<string, double, double, double, double>)
             ((id, x, y, width, height) =>
             {
                var bar = new UIProgressBar(
                   id,
                   () => new Rectangle(
                      (int)(x < 0 ? _form.Width + x : x),
                      (int)(y < 0 ? _form.Height + y : y),
                      (int)(width < 0 ? _form.Width + width : width),
                      (int)height));
                _uiElementManager.Add(bar);
             }));



         SetGlobal("update_progress", (Action<string, double>) ((id, value) => { _uiElementManager.UpdateProgressBar(id, value); }));
         SetGlobal("update_label", (Action<string, string>)((id, text) => { _uiElementManager.UpdateLabel(id, text); }));
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
            var method = typeof(Lua).GetMethod(nameof(ExposeToLua))!
            .MakeGenericMethod(type);
            method.Invoke(this, [null, null]);
         }
      }

      public void ClearImageCache() {
         foreach (var img in _imageCache.Values)
            img?.Dispose();
         _imageCache.Clear();
      }

      public void LoadTheme(string themeName) {
         _currentTheme = themeName;
         ClearImageCache();
         CallFunction(LuaKeys.Theme.LoadTheme, themeName);
         CallDynFunction(GetTheme().Get("init"));
      }

      public void ReInitTheme() {
         if (!string.IsNullOrEmpty(_currentTheme)) {
            _uiElementManager.ClearAll();
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

      public ThemeBackground GetBackground() {
         var bg = GetThemeTable("background");
         return new ThemeBackground {
            TopLeft = LoadImage(bg, "top_left"),
            TopCenter = LoadImage(bg, "top_center"),
            TopRight = LoadImage(bg, "top_right"),
            RightCenter = LoadImage(bg, "right_center"),
            BottomRight = LoadImage(bg, "bottom_right"),
            BottomCenter = LoadImage(bg, "bottom_center"),
            BottomLeft = LoadImage(bg, "bottom_left"),
            LeftCenter = LoadImage(bg, "left_center"),
            FillColor = ToColor(bg.Get("fill_color"), Color.Black)
         };
      }

      public ThemeLayout GetLayout() {
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

      private Image LoadImage(Table table, string key) {
         string file = table.Get(key).CastToString();
         if (string.IsNullOrWhiteSpace(file))
            return new Bitmap(1, 1);

         if (_imageCache.TryGetValue(file, out var cached))
            return cached;

         string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);
         if (!File.Exists(path))
            return new Bitmap(1, 1);

         using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
         var img = Image.FromStream(fs);
         _imageCache[file] = img;
         return img;
      }

      private Color ToColor(DynValue val, Color fallback) =>
         val.Type == DataType.String ? ColorTranslator.FromHtml(val.String) : fallback;
   }
}
