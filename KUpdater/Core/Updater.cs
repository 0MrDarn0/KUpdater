// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace KUpdater.Core {
    public class Updater {
        private readonly IUpdateSource _source;
        private readonly string _baseUrl;
        private readonly string _rootDirectory;
        private readonly string _localVersionFile;

        public event Action<string>? StatusChanged;
        public event Action<int>? ProgressChanged;
        public event Action<string>? ChangelogChanged;

        public Updater(IUpdateSource source, string baseUrl, string rootDirectory) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
            _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            _localVersionFile = Path.Combine(_rootDirectory, "version.txt");
        }

        private string MetadataUrl => _baseUrl + "update.json";
        private string ChangelogUrl => _baseUrl + "changelog.txt";

        public async Task RunUpdateAsync() {
            try {
                StatusChanged?.Invoke(Localization.Translate("status.waiting"));

                var metadata = await GetMetadataAsync();
                var currentVersion = GetLocalVersion();
                var changelogData = await GetChangelogAsync();

                ChangelogChanged?.Invoke(changelogData);

                bool needsUpdate = currentVersion != metadata.Version;

                if (!needsUpdate) {
                    foreach (var file in metadata.Files) {
                        var localFile = new FileInfo(Path.Combine(_rootDirectory, file.Path));
                        if (!localFile.Exists || ComputeSha256(localFile.FullName) != file.Sha256) {
                            needsUpdate = true;
                            break;
                        }
                    }
                }

                if (!needsUpdate) {
                    StatusChanged?.Invoke(Localization.Translate("status.up_to_date", currentVersion));
                    return;
                }

                StatusChanged?.Invoke(Localization.Translate("status.update_required", currentVersion, metadata.Version));
                await DownloadAndExtractAsync(metadata);

                SaveLocalVersion(metadata.Version);
                StatusChanged?.Invoke(Localization.Translate("status.update_applied"));
            }
            catch (Exception ex) {
                StatusChanged?.Invoke(Localization.Translate("status.update_failed", ex.Message));
            }
        }

        private async Task<UpdateMetadata> GetMetadataAsync() {
            var json = await _source.GetMetadataJsonAsync(MetadataUrl);
            return JsonSerializer.Deserialize<UpdateMetadata>(json)!;
        }

        public async Task<string> GetChangelogAsync() {
            return await _source.GetChangelogAsync(ChangelogUrl);
        }

        private string GetLocalVersion() {
            if (!File.Exists(_localVersionFile))
                return "0.0.0";
            return File.ReadAllText(_localVersionFile).Trim();
        }

        private void SaveLocalVersion(string version) {
            File.WriteAllText(_localVersionFile, version);
        }

        private async Task DownloadAndExtractAsync(UpdateMetadata metadata) {
            string tempZip = Path.Combine(Path.GetTempPath(), "update.zip");

            StatusChanged?.Invoke(Localization.Translate("status.downloading_pkg"));

            await using (var stream = await _source.GetPackageStreamAsync(metadata.PackageUrl))
            await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None)) {
                byte[] buffer = new byte[8192];
                long totalRead = 0;

                long totalLength = await _source.GetPackageSizeAsync(metadata.PackageUrl) ?? -1;

                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await fs.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (totalLength > 0) {
                        int percent = (int)((totalRead * 100L) / totalLength);
                        ProgressChanged?.Invoke(percent);
                    }
                }
            }

            StatusChanged?.Invoke(Localization.Translate("status.extracting_files"));

            using (var archive = ZipFile.OpenRead(tempZip)) {
                int count = archive.Entries.Count;
                int current = 0;

                foreach (var entry in archive.Entries) {
                    string destinationPath = Path.Combine(_rootDirectory, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);

                    // Hash prüfen
                    var metaFile = Array.Find(metadata.Files, f =>
                  string.Equals(f.Path.Replace("\\", "/"), entry.FullName.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase));

                    if (metaFile != null) {
                        string actualHash = ComputeSha256(destinationPath);
                        if (!string.Equals(actualHash, metaFile.Sha256, StringComparison.OrdinalIgnoreCase)) {
                            throw new InvalidDataException(Localization.Translate("error.hash_mismatch", entry.FullName));
                        }
                    }

                    current++;
                    int percent = 100 * current / count;
                    ProgressChanged?.Invoke(percent);
                }
            }

            File.Delete(tempZip);
            StatusChanged?.Invoke(Localization.Translate("status.update_complete"));
        }

        private static string ComputeSha256(string filePath) {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
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
