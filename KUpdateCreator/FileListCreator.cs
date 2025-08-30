using System.Xml;

namespace KUpdateCreator {
   public static class FileListCreator {
      public static string GetSHA256(string filePath) {
         using var sha = System.Security.Cryptography.SHA256.Create();
         using var stream = File.OpenRead(filePath);
         var hash = sha.ComputeHash(stream);
         return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
      }

      public static void WriteFileList(
          System.Collections.Generic.List<(string File, string Hash)> entries,
          string outputPath) {
         using var writer = new XmlTextWriter(outputPath, System.Text.Encoding.UTF8)
            {
            Formatting = Formatting.Indented
         };
         writer.WriteStartDocument();
         writer.WriteStartElement("Filelist");

         foreach (var (file, hash) in entries) {
            writer.WriteStartElement("Fileinfo");
            writer.WriteElementString("File", file);
            writer.WriteElementString("Hash", hash);
            writer.WriteEndElement();
         }

         writer.WriteEndElement();
         writer.WriteEndDocument();
      }
   }
}
