using KUpdater.UI;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting {

   public class MainFormTheme : Lua, ITheme {
      private readonly UIElementManager _uiElementManager;
      private readonly Dictionary<string, Image> _imageCache = new();
      private string? _currentTheme;

      public MainFormTheme(UIElementManager uiElementManager) : base("theme_loader.lua") {
         _uiElementManager = uiElementManager;
         SetGlobal(LuaKeys.Theme.ThemeDir, Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes").Replace("\\", "/"));
         LoadTheme("main_form");
      }

      protected override void RegisterGlobals() {
         SetGlobal(LuaKeys.UI.GetWindowSize, (Func<DynValue>)(() => {
            var form = MainForm.Instance;
            return DynValue.NewTuple(
               DynValue.NewNumber(form?.Width ?? 0),
               DynValue.NewNumber(form?.Height ?? 0));
         }));


         SetGlobal(LuaKeys.UI.AddLabel, (Action<string, double, double, string, string, double, string>)
            ((text, x, y, colorHex, fontName, fontSize, fontStyle) => {
               Color color = ColorTranslator.FromHtml(colorHex);
               if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                  style = FontStyle.Regular;
               Font font = new(fontName, (float)fontSize, style);
               _uiElementManager.Add(new UILabel(() => new Rectangle((int)x, (int)y,
                  TextRenderer.MeasureText(text, font).Width,
                  TextRenderer.MeasureText(text, font).Height), text, font, color));
            }));

         SetGlobal(LuaKeys.UI.AddButton, (Action<string, double, double, double, double, string, double, string, string, string, DynValue>)
            ((text, x, y, width, height, fontName, fontSize, fontStyle, colorHex, id, callback) => {
               Color color = ColorTranslator.FromHtml(colorHex);
               if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                  style = FontStyle.Regular;
               Font font = new(fontName, (float)fontSize, style);
               var button = new UIButton(() => new Rectangle((int)x, (int)y, (int)width, (int)height),
                  text, font, id, () => {
                     if (callback.Type == DataType.Function)
                        _script.Call(callback);
                  });
               _uiElementManager.Add(button);
            }));

         SetGlobal(LuaKeys.Actions.StartGame, (Action)(() => GameLauncher.StartGame()));
         SetGlobal(LuaKeys.Actions.OpenSettings, (Action)(() => GameLauncher.OpenSettings()));
         SetGlobal(LuaKeys.Actions.ApplicationExit, (Action)(() => Application.Exit()));
      }

      public void LoadTheme(string themeName) {
         _currentTheme = themeName;
         CallFunction(LuaKeys.Theme.LoadTheme, themeName);

         var themeTable = GetTheme();
         var initFunc = themeTable.Get("init");
         if (initFunc.Type == DataType.Function)
            _script.Call(initFunc);
      }

      public void ReInitTheme() {
         if (!string.IsNullOrEmpty(_currentTheme)) {
            _uiElementManager.ClearLabels();
            _uiElementManager.ClearButtons();
            LoadTheme(_currentTheme);
         }
      }

      public Table GetTheme() => CallFunction(LuaKeys.Theme.GetTheme).Table;

      public ThemeBackground GetBackground() {
         var bg = GetTheme().Get("background").Table;
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
         var layout = GetTheme().Get("layout").Table;
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
