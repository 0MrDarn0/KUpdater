namespace KUpdater.UI {
   public static class UIResources {
      public static string PathFor(string fileName) =>
          Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);
   }
}
