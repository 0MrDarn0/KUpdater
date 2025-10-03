// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using SkiaSharp;

namespace KUpdater.Utility {
    public class ResourceManager : IDisposable {
        private readonly string _basePath;
        private readonly Dictionary<string, Image> _imageCache = [];
        private readonly Dictionary<string, Icon> _iconCache = [];
        private readonly Dictionary<string, string> _textCache = [];
        private readonly Dictionary<string, byte[]> _binaryCache = [];
        private readonly Dictionary<string, SKBitmap> _skiaCache = [];
        private readonly Dictionary<(string Family, float Size, FontStyle Style), Font> _fontCache = [];

        public ResourceManager(string? basePath = null) {
            _basePath = basePath ?? Paths.ResFolder;
        }

        private string ResolvePath(string fileName) => Path.Combine(_basePath, fileName);

        private bool TryResolvePath(string fileName, out string path) {
            path = ResolvePath(fileName);
            return File.Exists(path);
        }

        // 🔹 Optionales Laden
        public Image? GetImage(string fileName) {
            if (_imageCache.TryGetValue(fileName, out var cached))
                return cached;

            if (!TryResolvePath(fileName, out var path))
                return LogNull<Image>("Image", fileName);

            try {
                var img = Image.FromFile(path);
                _imageCache[fileName] = img;
                return img;
            }
            catch (Exception ex) {
                return LogError<Image>("Image", fileName, ex);
            }
        }

        public Icon? GetIcon(string fileName) {
            if (_iconCache.TryGetValue(fileName, out var cached))
                return cached;

            if (!TryResolvePath(fileName, out var path))
                return LogNull<Icon>("Icon", path);

            try {
                using var stream = File.OpenRead(path);
                var icon = new Icon(stream);
                _iconCache[fileName] = icon;
                return icon;
            }
            catch (Exception ex) {
                return LogError<Icon>("Icon", fileName, ex);
            }
        }

        public string? GetText(string fileName) {
            if (_textCache.TryGetValue(fileName, out var cached))
                return cached;

            if (!TryResolvePath(fileName, out var path))
                return LogNull<string>("Text", path);

            try {
                var content = File.ReadAllText(path);
                _textCache[fileName] = content;
                return content;
            }
            catch (Exception ex) {
                return LogError<string>("Text", fileName, ex);
            }
        }

        public byte[]? GetBinary(string fileName) {
            if (_binaryCache.TryGetValue(fileName, out var cached))
                return cached;

            if (!TryResolvePath(fileName, out var path))
                return LogNull<byte[]>("Binary", path);

            try {
                var data = File.ReadAllBytes(path);
                _binaryCache[fileName] = data;
                return data;
            }
            catch (Exception ex) {
                return LogError<byte[]>("Binary", fileName, ex);
            }
        }

        public SKBitmap GetSkiaBitmap(string fileName) {
            if (_skiaCache.TryGetValue(fileName, out var cached))
                return cached;

            var img = GetImage(fileName);
            if (img is null)
                return new SKBitmap(1, 1);

            var skBmp = img.ToSKBitmap();
            _skiaCache[fileName] = skBmp;
            return skBmp;
        }


        public Font GetFont(string family, float size, string style) {
            var fontStyle = style switch
         {
             "Bold" => FontStyle.Bold,
             "Italic" => FontStyle.Italic,
             "Regular" => FontStyle.Regular,
             "Underline" => FontStyle.Underline,
             _ => FontStyle.Regular
         };

            var key = (family, size, fontStyle);
            if (_fontCache.TryGetValue(key, out var cached))
                return cached;

            try {
                var font = new Font(family, size, fontStyle);
                _fontCache[key] = font;
                return font;
            }
            catch (Exception ex) {
                return LogError<Font>("Font", $"{family}, {size}, {style}", ex)!;
            }
        }


        // 🔹 Verpflichtendes Laden
        public Image RequireImage(string fileName) => GetImage(fileName) ?? Throw<Image>("Image", fileName);
        public Icon RequireIcon(string fileName) => GetIcon(fileName) ?? Throw<Icon>("Icon", fileName);
        public string RequireText(string fileName) => GetText(fileName) ?? Throw<string>("Text", fileName);
        public byte[] RequireBinary(string fileName) => GetBinary(fileName) ?? Throw<byte[]>("Binary", fileName);
        public SKBitmap RequireSkiaBitmap(string fileName) => GetSkiaBitmap(fileName) ?? new SKBitmap(1, 1);
        public Font RequireFont(string family, float size, string style) => GetFont(family, size, style) ?? throw new InvalidOperationException($"Font not available: {family}, {size}, {style}");

        // 🔹 Cleanup
        public void Dispose() {
            foreach (var img in _imageCache.Values)
                img.Dispose();
            foreach (var icon in _iconCache.Values)
                icon.Dispose();
            foreach (var bmp in _skiaCache.Values)
                bmp.Dispose();
            foreach (var font in _fontCache.Values)
                font.Dispose();

            _imageCache.Clear();
            _iconCache.Clear();
            _skiaCache.Clear();
            _textCache.Clear();
            _binaryCache.Clear();
            _fontCache.Clear();
        }

        // 🔹 Internes Logging & Fehlerhandling
        private T? LogError<T>(string type, string file, Exception ex) {
            Debug.WriteLine($"[AssetManager] Error loading {type} '{file}': {ex.Message}");
            return default;
        }

        private T? LogNull<T>(string type, string path) {
            Debug.WriteLine($"[AssetManager] {type} not found: {path}");
            return default;
        }

        private T Throw<T>(string type, string file)
            => throw new FileNotFoundException($"Required {type} not found: {file}", Path.Combine(_basePath, file));
    }
}
