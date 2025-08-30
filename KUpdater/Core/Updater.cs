using System.Security.Cryptography;
using System.Xml;

public class Updater {
   private readonly HttpClient _httpClient = new HttpClient();
   private readonly string _updateUrl;
   private readonly List<string> _toDownload = new List<string>();

   public Updater(string updateUrl) {
      _updateUrl = updateUrl.TrimEnd('/') + "/";
   }

   public Task<int> CompareFilesAsync(string xmlPath) {
      if (!File.Exists(xmlPath))
         throw new FileNotFoundException("Dateiliste nicht gefunden", xmlPath);

      var doc = new XmlDocument();
      doc.Load(xmlPath);

      var root = doc.DocumentElement
            ?? throw new InvalidOperationException("XML hat kein Root-Element.");

      var allFiles = root.GetElementsByTagName("Fileinfo");

      foreach (XmlNode n in allFiles) {
         var fileNode = n["File"];
         var hashNode = n["Hash"];
         if (fileNode == null || hashNode == null)
            continue;

         string fileName = fileNode.InnerText;
         string fileHash = hashNode.InnerText;

         if (!File.Exists(fileName) || GetSHA256HashFromFile(fileName) != fileHash) {
            _toDownload.Add(fileName);
         }
      }

      return Task.FromResult(_toDownload.Count);
   }

   public Task DeleteFilesAsync(string xmlPath) {
      if (!File.Exists(xmlPath))
         return Task.CompletedTask;

      var doc = new XmlDocument();
      doc.Load(xmlPath);

      var root = doc.DocumentElement
            ?? throw new InvalidOperationException("XML hat kein Root-Element.");

      var allFiles = root.GetElementsByTagName("Fileinfo");

      foreach (XmlNode n in allFiles) {
         var fileNode = n["File"];
         var hashNode = n["Hash"];
         if (fileNode == null || hashNode == null)
            continue;

         string fileName = fileNode.InnerText;
         string fileHash = hashNode.InnerText;

         if (File.Exists(fileName) && GetSHA256HashFromFile(fileName) == fileHash) {
            File.Delete(fileName);
         }
      }

      File.Delete(xmlPath);
      return Task.CompletedTask;
   }

   public async Task DownloadUpdatesAsync(
       IProgress<(string file, double percent, double speed)>? progress,
       CancellationToken token = default) {
      foreach (var file in _toDownload) {
         string fullUrl = _updateUrl + file.Replace("\\", "/");
         string? dir = Path.GetDirectoryName(file);

         if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

         var sw = System.Diagnostics.Stopwatch.StartNew();

         using var response = await _httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead, token);
         response.EnsureSuccessStatusCode();

         var totalBytes = response.Content.Headers.ContentLength ?? -1L;
         var canReportProgress = totalBytes != -1;

         await using var stream = await response.Content.ReadAsStreamAsync(token);
         await using var fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);

         var buffer = new byte[81920];
         long totalRead = 0;
         int read;
         while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0) {
            await fs.WriteAsync(buffer.AsMemory(0, read), token);
            totalRead += read;

            if (canReportProgress) {
               double percent = (totalRead * 1d / totalBytes) * 100;
               double speed = (totalRead / 1024d) / sw.Elapsed.TotalSeconds; // KB/s
               progress?.Report((file, percent, speed));
            }
         }
      }
   }

   private static string GetSHA256HashFromFile(string fileName) {
      using var sha = SHA256.Create();
      using var stream = File.OpenRead(fileName);
      var hashBytes = sha.ComputeHash(stream);
      return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
   }
}
