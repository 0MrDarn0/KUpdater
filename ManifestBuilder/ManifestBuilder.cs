using System.Text.Json;

public record ManifestEntry(string File, string Hash, long Size);

public static class ManifestBuilder {
   public static async Task GenerateManifestAsync(string rootFolder, string outputPath) {
      var entries = new List<ManifestEntry>();

      foreach (var file in Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories)) {
         var fileName = Path.GetFileName(file);
         var relativePath = Path.GetRelativePath(rootFolder, file).Replace("\\", "/");

         // Skip manifest.json (existing one)
         if (fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            continue;

         // Skip KUpdater.exe
         if (fileName.Equals("KUpdater.exe", StringComparison.OrdinalIgnoreCase))
            continue;

         // Skip any file inside the KUpdater folder
         if (relativePath.StartsWith("KUpdater/", StringComparison.OrdinalIgnoreCase))
            continue;

         string hash = HashHelper.ComputeFileHash(file);
         long size = new FileInfo(file).Length;

         entries.Add(new ManifestEntry(relativePath, hash, size));
      }

      var options = new JsonSerializerOptions { WriteIndented = true };
      string json = JsonSerializer.Serialize(entries, options);

      if (File.Exists(outputPath))
         File.Delete(outputPath);

      await File.WriteAllTextAsync(outputPath, json);

      Console.WriteLine($"✅ Manifest generated: {outputPath} ({entries.Count} entries)");
   }
}
