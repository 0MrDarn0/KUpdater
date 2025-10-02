using MoonSharp.Interpreter;

namespace KUpdater.Extensions {
   public static class LuaExtensions {
      // 🔹 Typensicheres Casten eines DynValue zu T
      public static T? As<T>(this DynValue val) {
         if (val.Type == DataType.UserData && val.UserData.Object is T typed)
            return typed;

         try {
            return val.ToObject<T>();
         }
         catch {
            return default;
         }
      }

      // 🔹 Ist der Wert "truthy" (Lua-Logik)
      public static bool IsTruthy(this DynValue val) {
         return val.Type != DataType.Nil && val.Type != DataType.Void &&
                !(val.Type == DataType.Boolean && val.Boolean == false);
      }

      // 🔹 Ist der Wert "falsy"
      public static bool IsFalsy(this DynValue val) {
         return !val.IsTruthy();
      }

      // 🔹 Versuche, einen String zu extrahieren
      public static string? AsString(this DynValue val) {
         return val.Type == DataType.String ? val.String : null;
      }

      // 🔹 Versuche, eine Zahl zu extrahieren
      public static double? AsNumber(this DynValue val) {
         return val.Type == DataType.Number ? val.Number : null;
      }

      // 🔹 Versuche, eine Lua-Tabelle zu extrahieren
      public static Table? AsTable(this DynValue val) {
         return val.Type == DataType.Table ? val.Table : null;
      }

      // 🔹 Versuche, eine Lua-Funktion zu extrahieren
      public static Closure? AsFunction(this DynValue val) {
         return val.Type == DataType.Function ? val.Function : null;
      }

      // 🔹 Versuche, ein UserData-Objekt zu extrahieren
      public static object? AsUserData(this DynValue val) {
         return val.Type == DataType.UserData ? val.UserData.Object : null;
      }
   }
}
