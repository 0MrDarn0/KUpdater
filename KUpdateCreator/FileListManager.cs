namespace KUpdateCreator {
   public class FileListManager {
      public IEnumerable<ListViewItem> LoadFiles(string folderPath) {
         foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)) {
            string relative = Path.GetRelativePath(folderPath, file);
            yield return new ListViewItem(relative) { Tag = file };
         }
      }

      public (int Count, long Size) GetSelectionStats(ListView listView) {
         int count = 0;
         long size = 0;

         foreach (ListViewItem item in listView.Items) {
            if (!item.Checked)
               continue;
            if (item.Tag is string path && File.Exists(path)) {
               count++;
               size += new FileInfo(path).Length;
            }
         }
         return (count, size);
      }
   }
}
