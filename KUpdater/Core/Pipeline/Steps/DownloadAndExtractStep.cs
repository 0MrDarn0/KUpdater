// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.IO.Compression;
using KUpdater.Core.Attributes;
using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline.Steps {
    [PipelineStep(30)]
    public class DownloadAndExtractStep : IUpdateStep {
        private readonly IUpdateSource _source;
        public string Name => "DownloadAndExtract";

        public DownloadAndExtractStep(IUpdateSource source) {
            _source = source;
        }

        public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager) {
            string tempZip = Path.Combine(Path.GetTempPath(), "update.zip");

            // Download
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.downloading_pkg")));
            await using (var stream = await _source.GetPackageStreamAsync(ctx.Metadata.PackageUrl))
            await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None)) {
                byte[] buffer = new byte[8192];
                long totalRead = 0;
                long totalLength = await _source.GetPackageSizeAsync(ctx.Metadata.PackageUrl) ?? -1;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await fs.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    if (totalLength > 0) {
                        int percent = (int)((totalRead * 100L) / totalLength);
                        eventManager.NotifyAll(new ProgressEvent(percent));
                    }
                }
            }

            // Extract
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.extracting_files")));
            using (var archive = ZipFile.OpenRead(tempZip)) {
                int count = archive.Entries.Count;
                int current = 0;
                foreach (var entry in archive.Entries) {
                    string destinationPath = Path.Combine(ctx.RootDirectory, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);

                    // Hash prüfen
                    var metaFile = Array.Find(ctx.Metadata.Files, f =>
                        string.Equals(f.Path.Replace("\\", "/"), entry.FullName.Replace("\\", "/"),
                            StringComparison.OrdinalIgnoreCase));

                    if (metaFile != null) {
                        var fileInfo = new FileInfo(destinationPath);
                        if (!fileInfo.VerifySha256(metaFile.Sha256)) {
                            throw new InvalidDataException(Localization.Translate("error.hash_mismatch", entry.FullName));
                        }
                    }

                    current++;
                    eventManager.NotifyAll(new ProgressEvent(100 * current / count));
                }
            }

            File.Delete(tempZip);
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.update_complete")));
        }
    }
}
