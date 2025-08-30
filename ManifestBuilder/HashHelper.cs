using System.Security.Cryptography;
using System.Text;

public static class HashHelper {
   public static string ComputeFileHash(string filePath, string algorithmName = "SHA256") {
      if (!File.Exists(filePath))
         throw new FileNotFoundException("File not found", filePath);

      using var algorithm = CreateAlgorithm(algorithmName);
      using var stream = File.OpenRead(filePath);
      var hashBytes = algorithm.ComputeHash(stream);
      return ConvertToHex(hashBytes);
   }

   public static bool VerifyFileHash(string filePath, string expectedHash, string algorithmName = "SHA256") {
      if (!File.Exists(filePath))
         return false;

      var actualHash = ComputeFileHash(filePath, algorithmName);
      return actualHash == expectedHash.ToLowerInvariant();
   }

   public static string ComputeStringHash(string input, string algorithmName = "SHA256") {
      if (input == null)
         throw new ArgumentNullException(nameof(input));

      using var algorithm = CreateAlgorithm(algorithmName);
      var bytes = Encoding.UTF8.GetBytes(input);
      var hashBytes = algorithm.ComputeHash(bytes);
      return ConvertToHex(hashBytes);
   }

   private static string ConvertToHex(byte[] bytes) {
      var sb = new StringBuilder(bytes.Length * 2);
      foreach (var b in bytes)
         sb.Append(b.ToString("x2"));
      return sb.ToString();
   }

   private static HashAlgorithm CreateAlgorithm(string name) =>
       name.ToUpperInvariant() switch {
          "SHA256" => SHA256.Create(),
          "SHA512" => SHA512.Create(),
          "SHA1" => SHA1.Create(),
          "MD5" => MD5.Create(), // ⚠ Not secure — avoid for sensitive data
          _ => throw new ArgumentException($"Unsupported algorithm: {name}")
       };
}