using MoonSharp.Interpreter;

public class Theme
{
   public string Title { get; set; } = "kUpdater";
   public Color FontColor { get; set; } = Color.Orange;
   public Point TitlePosition { get; set; } = new(20, 10);
   public Font TitleFont { get; set; } = new("Segoe UI", 24f, FontStyle.Regular);

   public ButtonStyle Button { get; set; } = new();

   public class ButtonStyle
   {
      public Color Normal { get; set; } = Color.Red;
      public Color Hover { get; set; } = Color.OrangeRed;
      public Color Pressed { get; set; } = Color.DarkRed;
      public Color FontColor { get; set; } = Color.White;
      public Font Font { get; set; } = new("Segoe UI", 14f, FontStyle.Regular);
   }
}
public class ThemeBackground
{
   public Image TopLeft, TopCenter, TopRight;
   public Image RightCenter, BottomRight, BottomCenter, BottomLeft, LeftCenter;
   public Color FillColor = Color.Black;
}

public static class LuaManager
{
   private static Script _script;
   private static string ScriptPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", fileName);

   public static void Init(string fileName)
   {
      string path = ScriptPath(fileName);

      if (!File.Exists(path))
         throw new FileNotFoundException($"Lua script not found: {path}");

      try
      {
         _script = new Script();
         _script.DoString(File.ReadAllText(path));
      }
      catch (SyntaxErrorException e)
      {
         Console.WriteLine($"Lua syntax error: {e.DecoratedMessage}");
      }

      // Setze THEME_DIR (Pfad zu Lua/themes)
      string themeDir = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", "themes");
      _script.Globals["THEME_DIR"] = themeDir.Replace("\\", "/"); // für Lua
   }

   public static string CallString(string functionName, params object[] args)
   {
      return _script.Call(_script.Globals[functionName], args).String;
   }

   public static Color CallColor(string functionName)
   {
      string hex = _script.Call(_script.Globals[functionName]).String;
      return ColorTranslator.FromHtml(hex);
   }

   public static Point CallPoint(string functionName)
   {
      var table = _script.Call(_script.Globals[functionName]).Table;
      int x = (int)table.Get("x").Number;
      int y = (int)table.Get("y").Number;
      return new Point(x, y);
   }

   public static string CallButtonColor(bool isHover, bool isPressed)
   {
      return _script.Call(_script.Globals["get_button_color"], isHover, isPressed).String;
   }

   public static Font CallFont(string functionName)
   {
      var table = _script.Call(_script.Globals[functionName]).Table;

      string name = table.Get("name").String ?? "Segoe UI";
      float size = (float)(table.Get("size").Number);
      string styleStr = table.Get("style").String ?? "Regular";

      if (!Enum.TryParse(styleStr, true, out FontStyle style))
         style = FontStyle.Regular;

      return new Font(name, size, style);
   }

   public static void LoadTheme(string themeName)
   {
      _script.Call(_script.Globals["load_theme"], themeName);
   }

   public static Table GetTheme()
   {
      return _script.Call(_script.Globals["get_theme"]).Table;
   }
   public static Font GetFontFromTable(Table table, string defaultName = "Segoe UI", float defaultSize = 24f, FontStyle defaultStyle = FontStyle.Regular)
   {
      string name = table.Get("name").CastToString() ?? defaultName;
      float size = (float)(table.Get("size").CastToNumber() ?? defaultSize);

      FontStyle style;
      try
      {
         style = Enum.Parse<FontStyle>(table.Get("style").CastToString() ?? defaultStyle.ToString(), true);
      }
      catch
      {
         style = defaultStyle;
      }

      return new Font(name, size, style);
   }
   public static Theme GetParsedTheme()
   {
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

   private static Color ToColor(DynValue val, Color fallback)
   {
      return val.Type == DataType.String ? ColorTranslator.FromHtml(val.String) : fallback;
   }

   private static Point ToPoint(DynValue val, Point fallback)
   {
      if (val.Type != DataType.Table) return fallback;
      var t = val.Table;
      int x = (int)(t.Get("x").CastToNumber() ?? fallback.X);
      int y = (int)(t.Get("y").CastToNumber() ?? fallback.Y);
      return new Point(x, y);
   }

   private static Font ToFont(DynValue val, Font fallback)
   {
      if (val.Type != DataType.Table) return fallback;
      var t = val.Table;

      string name = t.Get("name").CastToString() ?? fallback.Name;
      float size = (float)(t.Get("size").CastToNumber() ?? fallback.Size);
      FontStyle style;

      try
      {
         style = Enum.Parse<FontStyle>(t.Get("style").CastToString() ?? fallback.Style.ToString(), true);
      }
      catch { style = fallback.Style; }

      return new Font(name, size, style);
   }

   public static ThemeBackground GetBackground()
   {
      var theme = GetTheme();
      var bg = theme.Get("background").Table;

      ThemeBackground result = new()
      {
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

      return result;
   }

   private static Image LoadImage(Table table, string key)
   {
      string file = table.Get(key).CastToString();
      if (string.IsNullOrWhiteSpace(file))
         throw new Exception($"Missing background image key: {key}");

      string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", file);

      if (!File.Exists(path))
         throw new FileNotFoundException($"Background image not found: {path}");

      return Image.FromFile(path);
   }

}
