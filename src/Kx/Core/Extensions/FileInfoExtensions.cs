// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Security.Cryptography;

namespace Kx.Core.Extensions;

public static class FileInfoExtensions {
    /// <summary>
    /// Computes the SHA256 hash of the file and returns it as an uppercase hex string.
    /// Synchronous convenience wrapper. Prefer the async variant for non-blocking I/O.
    /// </summary>
    public static string ComputeSha256(this FileInfo file) {
        using var sha = SHA256.Create();
        using var stream = file.OpenRead();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    /// <summary>
    /// Computes the SHA256 hash of the file asynchronously and returns it as an uppercase hex string.
    /// Uses IncrementalHash and asynchronous stream reads to avoid blocking the calling thread.
    /// </summary>
    public static async Task<string> ComputeSha256Async(this FileInfo file, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.Exists)
            throw new FileNotFoundException("File not found", file.FullName);

        // Buffer size similar to Stream.CopyTo default
        const int bufferSize = 81920;
        var buffer = new byte[bufferSize];

        using var stream = file.OpenRead();
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0) {
            ct.ThrowIfCancellationRequested();
            hasher.AppendData(buffer, 0, bytesRead);
        }

        var hash = hasher.GetHashAndReset();
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    /// <summary>
    /// Verifies the file against an expected SHA256 hash.
    /// Synchronous convenience wrapper.
    /// </summary>
    public static bool VerifySha256(this FileInfo file, string expectedHash) {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(expectedHash);
        if (!file.Exists)
            return false;
        return file.ComputeSha256().Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies the file against an expected SHA256 hash asynchronously.
    /// Returns false if file does not exist.
    /// </summary>
    public static async Task<bool> VerifySha256Async(this FileInfo file, string expectedHash, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(expectedHash);
        if (!file.Exists)
            return false;

        var actual = await file.ComputeSha256Async(ct).ConfigureAwait(false);
        return actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
