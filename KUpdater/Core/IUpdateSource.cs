// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core;

public interface IUpdateSource {
    Task<string> GetMetadataJsonAsync(string metadataUrl);
    Task<Stream> GetPackageStreamAsync(string packageUrl);
    Task<long?> GetPackageSizeAsync(string packageUrl);
    Task<string> GetChangelogAsync(string changelogUrl);
}
