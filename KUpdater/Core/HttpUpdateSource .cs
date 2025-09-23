using System.Net.Http;

namespace KUpdater.Core {

   public class HttpUpdateSource : IUpdateSource {
      private readonly HttpClient _http;

      public HttpUpdateSource(HttpClient? httpClient = null) {
         _http = httpClient ?? new HttpClient();
      }

      public async Task<string> GetMetadataJsonAsync(string metadataUrl) {
         return await _http.GetStringAsync(metadataUrl);
      }

      public async Task<Stream> GetPackageStreamAsync(string packageUrl) {
         var response = await _http.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead);
         response.EnsureSuccessStatusCode();
         return await response.Content.ReadAsStreamAsync();
      }
   }

}
