using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;

class UpdateBuilder {
   static void Main(string[] args) {
      string updateFolder = Path.Combine(Directory.GetCurrentDirectory(), "Update");
      string outputZip = Path.Combine(Directory.GetCurrentDirectory(), "update.zip");
      string outputJson = Path.Combine(Directory.GetCurrentDirectory(), "update.json");

      if (!Directory.Exists(updateFolder)) {
         Console.WriteLine("Update-Ordner nicht gefunden!");
         return;
      }

      // 1. ZIP erstellen
      if (File.Exists(outputZip))
         File.Delete(outputZip);
      ZipFile.CreateFromDirectory(updateFolder, outputZip, CompressionLevel.Optimal, includeBaseDirectory: false);
      Console.WriteLine("ZIP erstellt: " + outputZip);

      // 2. Hashes berechnen
      var files = new List<UpdateFile>();
      foreach (var file in Directory.GetFiles(updateFolder, "*", SearchOption.AllDirectories)) {
         string relativePath = Path.GetRelativePath(updateFolder, file);
         string hash = ComputeSha256(file);
         files.Add(new UpdateFile { Path = relativePath.Replace("\\", "/"), Sha256 = hash });
      }

      // 3. Version hochzählen
      string versionFile = Path.Combine(Directory.GetCurrentDirectory(), "version.txt");
      string version = "1.0.0";
      if (File.Exists(versionFile)) {
         version = File.ReadAllText(versionFile).Trim();
         version = IncrementVersion(version);
      }
      File.WriteAllText(versionFile, version);

      // 4. JSON schreiben
      var metadata = new UpdateMetadata
        {
         Version = version,
         PackageUrl = "http://darn.bplaced.net/KUpdater/update.zip",
         Files = files
      };

      var options = new JsonSerializerOptions { WriteIndented = true };
      File.WriteAllText(outputJson, JsonSerializer.Serialize(metadata, options));

      Console.WriteLine("JSON erstellt: " + outputJson);
      Console.WriteLine("Neue Version: " + version);
   }

   private static string ComputeSha256(string filePath) {
      using var sha = SHA256.Create();
      using var stream = File.OpenRead(filePath);
      var hash = sha.ComputeHash(stream);
      return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
   }

   private static string IncrementVersion(string version) {
      var parts = version.Split('.');
      if (parts.Length < 3)
         return "1.0.0";

      if (int.TryParse(parts[2], out int patch)) {
         patch++;
         return $"{parts[0]}.{parts[1]}.{patch}";
      }
      return "1.0.0";
   }
}

public class UpdateMetadata {
   public string Version { get; set; } = "";
   public string PackageUrl { get; set; } = "";
   public List<UpdateFile> Files { get; set; } = new();
}

public class UpdateFile {
   public string Path { get; set; } = "";
   public string Sha256 { get; set; } = "";
}
