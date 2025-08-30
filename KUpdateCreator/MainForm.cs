using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace KUpdateCreator {
   public partial class MainForm : Form {
      private TabControl tabControl = null!;
      private Button btnOpenClientFolder = null!;
      private Button btnCopyToUpdate = null!;
      private ListView fileListView = null!;
      private ToolStripStatusLabel statusLabel = null!;
      private SKGLControl skglControl = null!;
      private Label previewMessageLabel = null!;

      private readonly FileListManager fileListManager = new();
      private readonly UpdateFileProcessor updateProcessor = new();
      private List<(string File, string Hash)> fileEntries = new();

      private string clientFolderPath = "";
      private float scrollOffsetY = 0;
      private const float lineHeight = 42;


      public MainForm() {
         InitializeComponent();
         Text = "🧵 KUpdateCreator Tool";
         Width = 1000;
         Height = 700;

         tabControl = new TabControl { Dock = DockStyle.Fill };
         Controls.Add(tabControl);

         CreateFileSelectionTab();
         CreatePreviewTab();
      }

      private void CreateFileSelectionTab() {
         var tab = new TabPage("📂 Select Files");

         var layout = new TableLayoutPanel
            {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
         };
         layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
         layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
         layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
         layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

         btnOpenClientFolder = new Button { Text = "Open Folder", Dock = DockStyle.Fill };
         btnOpenClientFolder.Click += BtnOpenClientFolder_Click;
         layout.Controls.Add(btnOpenClientFolder, 0, 0);

         btnCopyToUpdate = new Button { Text = "Copy Selected Files", Dock = DockStyle.Fill, Visible = false };
         btnCopyToUpdate.Click += BtnCopyToUpdate_Click;
         layout.Controls.Add(btnCopyToUpdate, 0, 1);

         fileListView = new ListView { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true };
         fileListView.Columns.Add("File", 600);
         fileListView.ItemChecked += FileListView_ItemChecked;
         layout.Controls.Add(fileListView, 0, 2);

         var statusStrip = new StatusStrip();
         statusLabel = new ToolStripStatusLabel("No files selected");
         statusStrip.Items.Add(statusLabel);
         layout.Controls.Add(statusStrip, 0, 3);

         tab.Controls.Add(layout);
         tabControl.TabPages.Add(tab);
      }

      private async void BtnOpenClientFolder_Click(object? sender, EventArgs e) {
         using var dialog = new FolderBrowserDialog();
         if (dialog.ShowDialog() != DialogResult.OK)
            return;

         clientFolderPath = dialog.SelectedPath;
         fileListView.Items.Clear();
         btnCopyToUpdate.Visible = false;
         statusLabel.Text = "Loading files...";

         await Task.Run(() => {
            var batch = new List<ListViewItem>();
            foreach (var item in fileListManager.LoadFiles(clientFolderPath)) {
               batch.Add(item);
               if (batch.Count >= 200) {
                  var toAdd = batch.ToArray();
                  batch.Clear();
                  fileListView.Invoke(() => fileListView.Items.AddRange(toAdd));
               }
            }
            if (batch.Count > 0)
               fileListView.Invoke(() => fileListView.Items.AddRange(batch.ToArray()));
         });

         statusLabel.Text = "Done loading files.";
      }

      private void FileListView_ItemChecked(object? sender, ItemCheckedEventArgs e) {
         var (count, size) = fileListManager.GetSelectionStats(fileListView);
         btnCopyToUpdate.Visible = count > 0;
         statusLabel.Text = $"{count} file(s) selected – Total size: {FormatBytes(size)}";
      }

      private void BtnCopyToUpdate_Click(object? sender, EventArgs e) {
         string updateDir = Path.Combine(Application.StartupPath, "update");
         fileEntries = updateProcessor.CopySelectedFiles(fileListView, updateDir);
         FileListCreator.WriteFileList(fileEntries, Path.Combine(Application.StartupPath, "filelist.xml"));

         scrollOffsetY = 0;
         skglControl.Invalidate();
         tabControl.SelectedIndex = 1;
         previewMessageLabel.Text = $"✅ {fileEntries.Count} file(s) copied and filelist.xml created.";
      }

      private void CreatePreviewTab() {
         var tab = new TabPage("🧾 Output");
         var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
         layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
         layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

         previewMessageLabel = new Label {
            Text = "",
            Dock = DockStyle.Fill,
            ForeColor = Color.LimeGreen,
            TextAlign = ContentAlignment.MiddleLeft
         };
         layout.Controls.Add(previewMessageLabel, 0, 0);

         skglControl = new SKGLControl { Dock = DockStyle.Fill };
         skglControl.PaintSurface += SkglControl_PaintSurface;
         skglControl.MouseWheel += SkglControl_MouseWheel;
         layout.Controls.Add(skglControl, 0, 1);

         tab.Controls.Add(layout);
         tabControl.TabPages.Add(tab);
      }

      private void SkglControl_MouseWheel(object? sender, MouseEventArgs e) {
         scrollOffsetY -= e.Delta / 2f;
         skglControl.Invalidate();
      }

      private void SkglControl_PaintSurface(object? sender, SKPaintGLSurfaceEventArgs e) {
         var canvas = e.Surface.Canvas;
         canvas.Clear(new SKColor(10, 10, 10));

         float yPosition = 60 + scrollOffsetY;
         var font = new SKFont { Size = 18, Edging = SKFontEdging.Alias };

         // Farbverlauf für Dateipfade (Grün)
         var pathShader = SKShader.CreateLinearGradient(
        new SKPoint(0, 0),
        new SKPoint(400, 0),
        new[] { new SKColor(0, 255, 0), new SKColor(0, 180, 0) },
        null,
        SKShaderTileMode.Clamp
    );

         // Farbverlauf für Hashes (Blau)
         var hashShader = SKShader.CreateLinearGradient(
        new SKPoint(0, 0),
        new SKPoint(400, 0),
        new[] { new SKColor(0, 180, 255), new SKColor(0, 100, 200) },
        null,
        SKShaderTileMode.Clamp
    );

         // Glow- und Text-Paints für Pfade
         var glowPathPaint = new SKPaint
    {
            Shader = pathShader,
            IsAntialias = true,
            ImageFilter = SKImageFilter.CreateBlur(4, 4),
            Style = SKPaintStyle.Fill
         };
         var textPathPaint = new SKPaint
    {
            Shader = pathShader,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
         };

         // Glow- und Text-Paints für Hashes
         var glowHashPaint = new SKPaint
    {
            Shader = hashShader,
            IsAntialias = true,
            ImageFilter = SKImageFilter.CreateBlur(4, 4),
            Style = SKPaintStyle.Fill
         };
         var textHashPaint = new SKPaint
    {
            Shader = hashShader,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
         };

         // Zeichnen
         foreach (var (file, hash) in fileEntries) {
            // Dateipfad
            canvas.DrawText(file, 10, yPosition, SKTextAlign.Left, font, glowPathPaint);
            canvas.DrawText(file, 10, yPosition, SKTextAlign.Left, font, textPathPaint);
            yPosition += 22;

            // Hash
            string hashText = $"Hash: {hash}";
            canvas.DrawText(hashText, 20, yPosition, SKTextAlign.Left, font, glowHashPaint);
            canvas.DrawText(hashText, 20, yPosition, SKTextAlign.Left, font, textHashPaint);
            yPosition += lineHeight;

            if (yPosition > e.BackendRenderTarget.Height + 100)
               break;
         }

         // Scrollbegrenzung
         float maxScroll = Math.Max(0, fileEntries.Count * lineHeight - e.BackendRenderTarget.Height);
         scrollOffsetY = Math.Max(-maxScroll, Math.Min(scrollOffsetY, 0));
      }



      private string FormatBytes(long bytes) {
         string[] sizes = { "B", "KB", "MB", "GB", "TB" };
         double len = bytes;
         int order = 0;
         while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
         }
         return $"{len:0.##} {sizes[order]}";
      }
   }
}