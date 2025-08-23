using KUpdater.UI;
using MoonSharp.Interpreter;

public class Theme {
   public string Title { get; set; } = "kUpdater";
   public Color FontColor { get; set; } = Color.Orange;
   public Point TitlePosition { get; set; } = new(20, 10);
   public Font TitleFont { get; set; } = new("Segoe UI", 24f, FontStyle.Regular);

   public ButtonStyle Button { get; set; } = new();

   public class ButtonStyle {
      public Color Normal { get; set; } = Color.Red;
      public Color Hover { get; set; } = Color.OrangeRed;
      public Color Pressed { get; set; } = Color.DarkRed;
      public Color FontColor { get; set; } = Color.White;
      public Font Font { get; set; } = new("Segoe UI", 14f, FontStyle.Regular);
      public string BaseName { get; set; } = "btn_default";
   }
}

public class ThemeBackground {
   public Image TopLeft, TopCenter, TopRight;
   public Image RightCenter, BottomRight, BottomCenter, BottomLeft, LeftCenter;
   public Color FillColor = Color.Black;
}

public static class LuaManager {
   private static Script _script;
   private static string? _currentTheme;
   private static string ScriptPath(string fileName) =>
       Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", fileName);

   public static void Init(string fileName) {
      string path = ScriptPath(fileName);

      if (!File.Exists(path))
         throw new FileNotFoundException($"Lua script not found: {path}");

      try {
         _script = new Script();

         // C#-Funktion in Lua registrieren
         _script.Globals["add_text"] = (Action<string, double, double, string, string, double, string>)(
             (text, x, y, colorHex, fontName, fontSize, fontStyle) => {
                Color color = ColorTranslator.FromHtml(colorHex);

                if (!Enum.TryParse(fontStyle, true, out FontStyle style))
                   style = FontStyle.Regular;

                Font font = new(fontName, (float)fontSize, style);
                Renderer.AddText(text, font, new Point((int)x, (int)y), color);
             });
         // Fenstergröße initial setzen
         //_script.Globals["FrameWidth"] = frameWidth;
         //_script.Globals["FrameHeight"] = frameHeight;
         UpdateWindowSizeFromForm();
         // THEME_DIR setzen
         string themeDir = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes");
         _script.Globals["THEME_DIR"] = themeDir.Replace("\\", "/");

         _script.DoString(File.ReadAllText(path));

      }
      catch (SyntaxErrorException e) {
         Console.WriteLine($"Lua syntax error: {e.DecoratedMessage}");
      }
   }

   public static void LoadTheme(string themeName) {
      _currentTheme = themeName;
      _script.Call(_script.Globals["load_theme"], themeName);
      UpdateWindowSizeFromForm();
      var themeTable = GetTheme();
      var initFunc = themeTable.Get("init");
      if (initFunc.Type == DataType.Function)
         _script.Call(initFunc);
   }

   public static void UpdateWindowSizeFromForm() {
      if (KUpdater.MainForm.Instance != null) {
         _script.Globals["FrameWidth"] = KUpdater.MainForm.Instance.Width;
         _script.Globals["FrameHeight"] = KUpdater.MainForm.Instance.Height;
      }
   }
   public static void ReInitTheme() {
      if (!string.IsNullOrEmpty(_currentTheme)) {
         KUpdater.UI.Renderer.ClearTexts();
         UpdateWindowSizeFromForm();
         _script.Call(_script.Globals["load_theme"], _currentTheme);
         var themeTable = GetTheme();
         var initFunc = themeTable.Get("init");
         if (initFunc.Type == DataType.Function)
            _script.Call(initFunc);
      }
   }
   public static Table GetTheme() =>
       _script.Call(_script.Globals["get_theme"]).Table;

   public static Theme GetParsedTheme() {
      var raw = GetTheme();
      Theme theme = new();

      theme.Title = raw.Get("window_title").CastToString() ?? theme.Title;
      theme.FontColor = ToColor(raw.Get("font_color"), theme.FontColor);
      theme.TitlePosition = ToPoint(raw.Get("title_position"), theme.TitlePosition);
      theme.TitleFont = ToFont(raw.Get("title_font"), theme.TitleFont);

      var btn = raw.Get("button").Table;
      theme.Button.Normal = ToColor(btn.Get("normal"), theme.Button.Normal);
      theme.Button.Hover = ToColor(btn.Get("hover"), theme.Button.Hover);
      theme.Button.Pressed = ToColor(btn.Get("pressed"), theme.Button.Pressed);
      theme.Button.FontColor = ToColor(btn.Get("font_color"), theme.Button.FontColor);
      theme.Button.Font = ToFont(btn.Get("font"), theme.Button.Font);

      return theme;
   }

   public static ThemeBackground GetBackground() {
      var theme = GetTheme();
      var bg = theme.Get("background").Table;

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

      string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);
      if (!File.Exists(path))
         throw new FileNotFoundException($"Background image not found: {path}");

      return Image.FromFile(path);
   }

   #endregion
}
