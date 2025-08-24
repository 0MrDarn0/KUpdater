using KUpdater.UI;
using MoonSharp.Interpreter;
using System.Diagnostics;

namespace KUpdater.Scripting {

   public class ThemeBackground {
      public Image TopLeft { get; set; } = new Bitmap(1, 1);
      public Image TopCenter { get; set; } = new Bitmap(1, 1);
      public Image TopRight { get; set; } = new Bitmap(1, 1);
      public Image RightCenter { get; set; } = new Bitmap(1, 1);
      public Image BottomRight { get; set; } = new Bitmap(1, 1);
      public Image BottomCenter { get; set; } = new Bitmap(1, 1);
      public Image BottomLeft { get; set; } = new Bitmap(1, 1);
      public Image LeftCenter { get; set; } = new Bitmap(1, 1);
      public Color FillColor { get; set; } = Color.Black;
   }

   public class ThemeLayout {
      public int TopWidthOffset { get; set; }
      public int BottomWidthOffset { get; set; }
      public int LeftHeightOffset { get; set; }
      public int RightHeightOffset { get; set; }
      public int FillPosOffset { get; set; }
      public int FillWidthOffset { get; set; }
      public int FillHeightOffset { get; set; }
   }


   public class LuaManager {
      // 🔹 Zentrale API-Definition
      private static class LuaApi {
         // Funktionen
         public const string AddLabel = "add_label";
         public const string AddButton = "add_button";
         public const string GetWindowSize = "get_window_size";
         public const string LoadTheme = "load_theme";
         public const string GetTheme = "get_theme";

         public const string StartGame = "start_game";
         public const string OpenSettings = "open_settings";
         public const string ApplicationExit = "application_exit";

         // Globale Variablen
         public const string ThemeDir = "THEME_DIR";
      }

      private static Script? _script;
      private static string? _currentTheme;
      private readonly UIManager _uiManager;
      private static readonly Dictionary<string, Image> _imageCache = new();

      private static Script ScriptInstance =>
          _script ?? throw new InvalidOperationException("LuaManager.Init() must be called before using LuaManager.");

      private static string ScriptPath(string fileName) =>
          Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", fileName);

      public LuaManager(UIManager uiManager) {
         _uiManager = uiManager;
      }

      public void Init(string fileName) {
         string path = ScriptPath(fileName);

         if (!File.Exists(path))
            throw new FileNotFoundException($"Lua script not found: {path}");

         try {
            _script = new();

            _script.Options.DebugPrint = s => {
               var msg = $"[{DateTime.Now:HH:mm:ss}] [Lua] >>> [{s}]";
               Debug.WriteLine(msg);
            };

            // Lua-Funktionen registrieren
            RegisterLuaFunctions();

            // THEME_DIR setzen
            string themeDir = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes");
            ScriptInstance.Globals[LuaApi.ThemeDir] = themeDir.Replace("\\", "/");

            ScriptInstance.DoString(File.ReadAllText(path));
         }
         catch (SyntaxErrorException e) {
            Debug.WriteLine($"Lua syntax error: {e.DecoratedMessage}");
         }
      }

      private static void SafeCall(DynValue function, params object[] args) {
         if (function.Type == DataType.Function) {
            try {
               ScriptInstance.Call(function, args);
            }
            catch (InterpreterException ex) {
               Debug.WriteLine($"Lua runtime error: {ex.DecoratedMessage}");
            }
         }
      }

      public void LoadTheme(string themeName) {
         _currentTheme = themeName;
         ScriptInstance.Call(ScriptInstance.Globals[LuaApi.LoadTheme], themeName);
         var themeTable = GetTheme();
         var initFunc = themeTable.Get("init");
         if (initFunc.Type == DataType.Function)
            ScriptInstance.Call(initFunc);
      }

      private void RegisterLuaFunctions() {
         RegisterWindowSizeFunction();
         RegisterAddLabelFunction();
         RegisterAddButtonFunction();
         RegisterStartGameFunction();
         RegisterOpenSettingsFunction();
         RegisterApplicationExitFunction();
      }

      private void RegisterAddLabelFunction() {
         ScriptInstance.Globals[LuaApi.AddLabel] =
             (Action<string, double, double, string, string, double, string>)
             ((text, x, y, colorHex, fontName, fontSize, fontStyle) => {
                Color color = ColorTranslator.FromHtml(colorHex);

                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;

                Font font = new(fontName, (float)fontSize, style);

                _uiManager.Add(new UILabel(
                       () => new Rectangle(
                           (int)x, (int)y,
                           TextRenderer.MeasureText(text, font).Width,
                           TextRenderer.MeasureText(text, font).Height),
                       text, font, color));
             });
      }

      private void RegisterAddButtonFunction() {
         // Lua: add_button("Text", x, y, width, height, "fontName", fontSize, "fontStyle", "color", "id", function() ... end)
         ScriptInstance.Globals[LuaApi.AddButton] =
             (Action<string, double, double, double, double, string, double, string, string, string, DynValue>)
             ((text, x, y, width, height, fontName, fontSize, fontStyle, colorHex, id, callback) => {
                Color color = ColorTranslator.FromHtml(colorHex);

                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;

                Font font = new(fontName, (float)fontSize, style);

                var button = new UIButton(
                () => new Rectangle((int)x, (int)y, (int)width, (int)height),
                text,
                font,
                id,
                () =>
                {
                   if (callback.Type == DataType.Function)
                      ScriptInstance.Call(callback);
                });

                _uiManager.Add(button);
             });
      }


      public void RegisterWindowSizeFunction() {
         ScriptInstance.Globals[LuaApi.GetWindowSize] = (Func<DynValue>)(() => {
            var form = KUpdater.MainForm.Instance;
            return DynValue.NewTuple(
                DynValue.NewNumber(form?.Width ?? 0),
                DynValue.NewNumber(form?.Height ?? 0)
            );
         });
      }

      private void RegisterStartGameFunction() {
         ScriptInstance.Globals[LuaApi.StartGame] = (Action)(() => {
            GameLauncher.StartGame();
         });
      }

      private void RegisterOpenSettingsFunction() {
         ScriptInstance.Globals[LuaApi.OpenSettings] = (Action)(() => {
            GameLauncher.OpenSettings();
         });
      }

      private void RegisterApplicationExitFunction() {
         ScriptInstance.Globals[LuaApi.ApplicationExit] = (Action)(() => {
            Application.Exit(); // oder Environment.Exit(0);
         });
      }


      public void ReInitTheme() {
         if (!string.IsNullOrEmpty(_currentTheme)) {
            _uiManager.ClearLabels();
            _uiManager.ClearButtons();
            ScriptInstance.Call(ScriptInstance.Globals[LuaApi.LoadTheme], _currentTheme);

            var themeTable = GetTheme();
            var initFunc = themeTable.Get("init");
            if (initFunc.Type == DataType.Function)
               ScriptInstance.Call(initFunc);
         }
      }

      public static Table GetTheme() =>
          ScriptInstance.Call(ScriptInstance.Globals[LuaApi.GetTheme]).Table;


      public static ThemeBackground GetBackground() {
         var theme = GetTheme();
         var bgVal = theme.Get("background");
         if (bgVal.Type != DataType.Table)
            throw new Exception("Theme is missing 'background' table.");
         var bg = bgVal.Table;


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

      public static ThemeLayout GetLayout() {
         var theme = GetTheme();
         var layoutVal = theme.Get("layout");
         if (layoutVal.Type != DataType.Table)
            throw new Exception("Theme is missing 'layout' table.");
         var layout = layoutVal.Table;


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



      #region Helper Methods

      private static Color ToColor(DynValue val, Color fallback) =>
          val.Type == DataType.String ? ColorTranslator.FromHtml(val.String) : fallback;

      private static Point ToPoint(DynValue val, Point fallback) {
         if (val.Type != DataType.Table)
            return fallback;

         var t = val.Table;
         int x = (int)(t.Get("x").CastToNumber() ?? fallback.X);
         int y = (int)(t.Get("y").CastToNumber() ?? fallback.Y);

         return new Point(x, y);
      }

      private static Font ToFont(DynValue val, Font fallback) {
         if (val.Type != DataType.Table)
            return fallback;

         var t = val.Table;
         string name = t.Get("name").CastToString() ?? fallback.Name;
         float size = (float)(t.Get("size").CastToNumber() ?? fallback.Size);

         if (!Enum.TryParse(t.Get("style").CastToString() ?? fallback.Style.ToString(), true, out FontStyle style))
            style = fallback.Style;

         return new Font(name, size, style);
      }



      private static Image LoadImage(Table table, string key) {
         string file = table.Get(key).CastToString();
         if (string.IsNullOrWhiteSpace(file))
            throw new Exception($"Missing background image key: {key}");

         if (_imageCache.TryGetValue(file, out var cached))
            return cached;

         string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);
         if (!File.Exists(path))
            throw new FileNotFoundException($"Background image not found: {path}");

         using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
         var img = Image.FromStream(fs);
         _imageCache[file] = img;
         return img;
      }


      #endregion
   }
}
