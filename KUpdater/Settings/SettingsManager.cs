using System.Diagnostics;
using System.Text.Json;

namespace KUpdater.Settings
{
   public class KUpdaterSettings
   {
      public string Title { get; set; } = "kUpdater";
      public string FontFamily { get; set; } = "Chiller";
      public float FontSize { get; set; } = 40f;
      public string FontStyle { get; set; } = "Italic";
      public string TitleColor { get; set; } = "#FFA500"; // Default: Orange
      public TitlePositionConfig TitlePosition { get; set; } = new TitlePositionConfig { X = 40, Y = -10 };
   }
   public class TitlePositionConfig
   {
      public int X { get; set; }
      public int Y { get; set; }
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
         string configFilePath = Path.Combine(MainForm.Paths.ResourceDir, _configFileName);
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
