class Program {
   static async Task Main(string[] args) {
      if (args.Length != 1) {
         Console.WriteLine("❌ Usage: ManifestBuilder.exe <path-to-update-folder>");
         return;
      }

      string updateFolder = args[0];

      if (!Directory.Exists(updateFolder)) {
         Console.WriteLine($"❌ Directory does not exist: {updateFolder}");
         return;
      }

      string manifestPath = Path.Combine(updateFolder, "manifest.json");

      try {
         await ManifestBuilder.GenerateManifestAsync(updateFolder, manifestPath);
      }
      catch (Exception ex) {
         Console.WriteLine($"❌ Error generating manifest: {ex.Message}");
      }
   }
}
