using System.Diagnostics;
using System.Text.Json;

namespace KUpdater.Settings
{
   public class KUpdaterSettings
   {
      public string Title { get; set; } = "kUpdater";
   }


   public static class SettingsManager
   {
      private const string _configFileName = "kUpdater.json";

      // Cache the JsonSerializerOptions instance to avoid creating a new one for every serialization operation
      private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
      {
         WriteIndented = true
      };

      public static KUpdaterSettings Load()
      {
         string configFilePath = Path.Combine(AppContext.BaseDirectory, _configFileName);
         if (!File.Exists(configFilePath))
         {
            KUpdaterSettings? defaultSettings = new();
            try
            {
               string json = JsonSerializer.Serialize(defaultSettings, _jsonSerializerOptions);
               File.WriteAllText(configFilePath, json);

            }
            catch (Exception ex)
            {
               Debug.WriteLine("Fehler beim Erstellen der Konfiguration: " + ex.Message);
            }

            return defaultSettings;
         }

         try
         {
            string json = File.ReadAllText(configFilePath);
            var settings = JsonSerializer.Deserialize<KUpdaterSettings>(json);
            return settings ?? new KUpdaterSettings();

         }
         catch (Exception ex)
         {
            Debug.WriteLine("Fehler beim Laden der Konfiguration: " + ex.Message);
            return new KUpdaterSettings();
         }

      }
   }
}
