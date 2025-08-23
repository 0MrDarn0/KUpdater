using System.Diagnostics;

namespace KUpdater.Assets {
   public static class AssetLoader {
      private static readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources");

      private static readonly Dictionary<string, Image> _imageCache = [];
      private static readonly Dictionary<string, Icon> _iconCache = [];
      private static readonly Dictionary<string, string> _textCache = [];
      private static readonly Dictionary<string, byte[]> _binaryCache = [];

      // -----------------------------------
      // OPTIONALES LADEN (kann null sein)
      // -----------------------------------

      public static Image? GetImage(string fileName) {
         if (_imageCache.TryGetValue(fileName, out var cachedImage))
            return cachedImage;

         string fullPath = Path.Combine(_basePath, fileName);
         if (!File.Exists(fullPath)) {
            Debug.WriteLine($"[AssetLoader] Image not found: {fullPath}");
            return null;
         }

         try {
            var img = Image.FromFile(fullPath);
            _imageCache[fileName] = img;
            return img;
         }
         catch (Exception ex) {
            Debug.WriteLine($"[AssetLoader] Error loading image {fileName}: {ex.Message}");
            return null;
         }
      }

      public static Icon? GetIcon(string fileName) {
         if (_iconCache.TryGetValue(fileName, out var cachedIcon))
            return cachedIcon;

         string fullPath = Path.Combine(_basePath, fileName);
         if (!File.Exists(fullPath)) {
            Debug.WriteLine($"[AssetLoader] Icon not found: {fullPath}");
            return null;
         }

         try {
            using var stream = File.OpenRead(fullPath);
            var icon = new Icon(stream);
            _iconCache[fileName] = icon;
            return icon;
         }
         catch (Exception ex) {
            Debug.WriteLine($"[AssetLoader] Error loading icon {fileName}: {ex.Message}");
            return null;
         }
      }

      public static string? GetText(string fileName) {
         if (_textCache.TryGetValue(fileName, out var cachedText))
            return cachedText;

         string fullPath = Path.Combine(_basePath, fileName);
         if (!File.Exists(fullPath)) {
            Debug.WriteLine($"[AssetLoader] Text file not found: {fullPath}");
            return null;
         }

         try {
            string content = File.ReadAllText(fullPath);
            _textCache[fileName] = content;
            return content;
         }
         catch (Exception ex) {
            Debug.WriteLine($"[AssetLoader] Error reading text {fileName}: {ex.Message}");
            return null;
         }
      }

      public static byte[]? GetBinary(string fileName) {
         if (_binaryCache.TryGetValue(fileName, out var cachedData))
            return cachedData;

         string fullPath = Path.Combine(_basePath, fileName);
         if (!File.Exists(fullPath)) {
            Debug.WriteLine($"[AssetLoader] Binary file not found: {fullPath}");
            return null;
         }

         try {
            byte[] data = File.ReadAllBytes(fullPath);
            _binaryCache[fileName] = data;
            return data;
         }
         catch (Exception ex) {
            Debug.WriteLine($"[AssetLoader] Error reading binary {fileName}: {ex.Message}");
            return null;
         }
      }

      // -----------------------------------
      // VERPFLICHTENDES LADEN (Exception bei Fehler)
      // -----------------------------------

      public static Image RequireImage(string fileName) {
         return GetImage(fileName)
             ?? throw new FileNotFoundException($"Pflichtbild nicht gefunden: {fileName}", Path.Combine(_basePath, fileName));
      }

      public static Icon RequireIcon(string fileName) {
         return GetIcon(fileName)
             ?? throw new FileNotFoundException($"Pflichticon nicht gefunden: {fileName}", Path.Combine(_basePath, fileName));
      }

      public static string RequireText(string fileName) {
         return GetText(fileName)
             ?? throw new FileNotFoundException($"Pflichttext nicht gefunden: {fileName}", Path.Combine(_basePath, fileName));
      }

      public static byte[] RequireBinary(string fileName) {
         return GetBinary(fileName)
             ?? throw new FileNotFoundException($"Pflichtdatei nicht gefunden: {fileName}", Path.Combine(_basePath, fileName));
      }
   }
}
