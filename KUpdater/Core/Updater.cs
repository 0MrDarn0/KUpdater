using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace KUpdater.Core {

   public class Updater {
      private readonly IUpdateSource _source;
      private readonly string _metadataUrl;
      private readonly string _rootDirectory;
      private readonly string _localVersionFile;

      public Updater(IUpdateSource source, string metadataUrl, string rootDirectory) {
         _source = source ?? throw new ArgumentNullException(nameof(source));
         _metadataUrl = metadataUrl ?? throw new ArgumentNullException(nameof(metadataUrl));
         _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
         _localVersionFile = Path.Combine(_rootDirectory, "version.txt");
      }

      public async Task RunUpdateAsync() {
         try {
            Console.WriteLine("Checking for updates...");

            var metadata = await GetMetadataAsync();
            var currentVersion = GetLocalVersion();

            bool needsUpdate = currentVersion != metadata.Version;

            if (!needsUpdate) {
               foreach (var file in metadata.Files) {
                  var localFile = new FileInfo(Path.Combine(_rootDirectory, file.Path));
                  if (!localFile.VerifySha256(file.Sha256)) {
                     needsUpdate = true;
                     break;
                  }
               }
            }

            if (!needsUpdate) {
               Console.WriteLine($"Already up to date (v{currentVersion}).");
               return;
            }

            Console.WriteLine($"Update required: {currentVersion} → {metadata.Version}");
            await DownloadAndExtractAsync(metadata.PackageUrl);

            SaveLocalVersion(metadata.Version);
            Console.WriteLine("Update applied successfully.");
         }
         catch (Exception ex) {
            Console.Error.WriteLine($"Update failed: {ex.Message}");
         }
      }

      private async Task<UpdateMetadata> GetMetadataAsync() {
         var json = await _source.GetMetadataJsonAsync(_metadataUrl);
         return JsonSerializer.Deserialize<UpdateMetadata>(json)!;
      }

      private string GetLocalVersion() {
         if (!File.Exists(_localVersionFile))
            return "0.0.0";
         return File.ReadAllText(_localVersionFile).Trim();
      }

      private void SaveLocalVersion(string version) {
         File.WriteAllText(_localVersionFile, version);
      }

      private async Task DownloadAndExtractAsync(string packageUrl) {
         string tempZip = Path.Combine(Path.GetTempPath(), "update.zip");

         await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
         await using (var stream = await _source.GetPackageStreamAsync(packageUrl)) {
            await stream.CopyToAsync(fs);
         }

         ZipFile.ExtractToDirectory(tempZip, _rootDirectory, overwriteFiles: true);
         File.Delete(tempZip);
      }
   }

   public class UpdateMetadata {
      public string Version { get; set; } = "";
      public string PackageUrl { get; set; } = "";
      public UpdateFile[] Files { get; set; } = Array.Empty<UpdateFile>();
   }

   public class UpdateFile {
      public string Path { get; set; } = "";
      public string Sha256 { get; set; } = "";
   }

}



   /*
   {
     "version": "1.2.3",
     "files": [
       { "path": "app.exe", "sha256": "ABC123..." },
       { "path": "lib.dll", "sha256": "DEF456..." }
     ],
     "packageUrl": "https://example.com/update.zip"
   }
    */
