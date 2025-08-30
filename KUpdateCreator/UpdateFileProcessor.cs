namespace KUpdateCreator {
   public class UpdateFileProcessor {
      public List<(string File, string Hash)> CopySelectedFiles(ListView listView, string updateDir) {
         var entries = new List<(string, string)>();
         Directory.CreateDirectory(updateDir);

         foreach (ListViewItem item in listView.Items) {
            if (!item.Checked)
               continue;
            if (item.Tag is not string sourcePath || string.IsNullOrWhiteSpace(sourcePath))
               continue;

            string relativePath = item.Text;
            string targetPath = Path.Combine(updateDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            File.Copy(sourcePath, targetPath, true);
            string hash = FileListCreator.GetSHA256(sourcePath);
            entries.Add((relativePath, hash));
         }
         return entries;
      }
   }
}
