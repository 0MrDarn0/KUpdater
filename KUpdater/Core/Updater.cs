using System.Net.Http.Json;

namespace KUpdater.Core {
   public record ManifestEntry(string File, string Hash, long Size);

   public class Updater {
      private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(10) };
      private string _baseUrl = "http://darn.bplaced.net/";
      private string _manifestEndpoint = "api/manifest.php";

      private readonly List<ManifestEntry> _toDownload = new();
      public Action<string>? Logger { get; set; } = Console.WriteLine;

      public Updater(string baseUrl, string manifestEndpoint = "api/manifest.php") {
         SetBaseUrl(baseUrl);
         _manifestEndpoint = manifestEndpoint;
      }

      public void SetBaseUrl(string url) {
         if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
             !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Base URL must start with http:// or https://");

         _baseUrl = url.TrimEnd('/') + "/";
      }

      private string GetManifestUrl() => _baseUrl + _manifestEndpoint;
      private string GetFileUrl(string file) => _baseUrl + file.Replace("\\", "/");

      public async Task RunAsync() {
         string localRoot = AppContext.BaseDirectory;
         if (await CompareFilesAsync(localRoot) > 0) {
            await DownloadUpdatesAsync(localRoot);
         }
      }

      public async Task<int> CompareFilesAsync(string localRoot) {
         _toDownload.Clear();

         var manifest = await _httpClient.GetFromJsonAsync<List<ManifestEntry>>(GetManifestUrl())
                          ?? throw new InvalidOperationException("Manifest could not be loaded.");

         foreach (var entry in manifest) {
            var localPath = Path.Combine(localRoot, entry.File);
            if (!File.Exists(localPath) || !Utility.HashHelper.VerifyFileHash(localPath, entry.Hash)) {
               _toDownload.Add(entry);
            }
         }

         return _toDownload.Count;
      }

      public async Task<bool> CheckForUpdatesAsync(string localRoot) {
         return await CompareFilesAsync(localRoot) > 0;
      }

      public async Task DownloadUpdatesAsync(string localRoot) {
         foreach (var entry in _toDownload) {
            var fileUrl = GetFileUrl(entry.File);
            var destPath = Path.Combine(localRoot, entry.File);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);

            if (!Utility.HashHelper.VerifyFileHash(destPath, entry.Hash)) {
               throw new InvalidOperationException($"Hash mismatch for {entry.File}");
            }
         }
      }

      public async Task PrintUpdatePlanAsync() {
         string localRoot = AppContext.BaseDirectory;
         _toDownload.Clear();

         var manifest = await _httpClient.GetFromJsonAsync<List<ManifestEntry>>(GetManifestUrl())
                  ?? throw new InvalidOperationException("Manifest could not be loaded.");

         Logger?.Invoke("🔍 Dry-Run: Checking for updates...");

         foreach (var entry in manifest) {
            var localPath = Path.Combine(localRoot, entry.File);
            if (!File.Exists(localPath) || !Utility.HashHelper.VerifyFileHash(localPath, entry.Hash)) {
               _toDownload.Add(entry);
               Logger?.Invoke($"📦 Will update: {entry.File} (Size: {entry.Size} bytes)");
            }
         }

         Logger?.Invoke(_toDownload.Count == 0
            ? "✅ All files are up to date."
            : $"\n📝 Total files to update: {_toDownload.Count}");
      }

      // ---------------- SYNC WRAPPERS ----------------
      public void Run() => RunAsync().GetAwaiter().GetResult();
      public bool CheckForUpdates(string localRoot) => CheckForUpdatesAsync(localRoot).GetAwaiter().GetResult();
      public int CompareFiles(string localRoot) => CompareFilesAsync(localRoot).GetAwaiter().GetResult();
      public void DownloadUpdates(string localRoot) => DownloadUpdatesAsync(localRoot).GetAwaiter().GetResult();
      public void PrintUpdatePlan() => PrintUpdatePlanAsync().GetAwaiter().GetResult();
   }
}
