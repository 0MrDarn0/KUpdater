using System;
using System.IO;
using System.Security.Cryptography;

public static class FileInfoExtensions {
   /// <summary>
   /// Computes the SHA256 hash of the file and returns it as an uppercase hex string.
   /// </summary>
   public static string ComputeSha256(this FileInfo file) {
      using var sha = SHA256.Create();
      using var stream = file.OpenRead();
      var hash = sha.ComputeHash(stream);
      return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
   }

   /// <summary>
   /// Verifies the file against an expected SHA256 hash.
   /// </summary>
   public static bool VerifySha256(this FileInfo file, string expectedHash) {
      if (!file.Exists)
         return false;
      return file.ComputeSha256().Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
   }
}