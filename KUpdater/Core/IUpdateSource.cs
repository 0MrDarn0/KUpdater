namespace KUpdater.Core {
   public interface IUpdateSource {
      Task<string> GetMetadataJsonAsync(string metadataUrl);
      Task<Stream> GetPackageStreamAsync(string packageUrl);
      Task<long?> GetPackageSizeAsync(string packageUrl);
   }

}
