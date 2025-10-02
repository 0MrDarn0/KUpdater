using System.Reflection;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting {

   public class UpdaterConfig {
      public string Url { get; set; } = string.Empty;
      public string Language { get; set; } = "en";
      public NetworkConfig Network { get; set; } = new();
   }

   public class NetworkConfig {
      public ProxyConfig Proxy { get; set; } = new();
   }

   public class ProxyConfig {
      public string Host { get; set; } = string.Empty;
      public int Port { get; set; }
   }


   public class LuaConfig<T> : Lua where T : new() {
      private readonly string _tableName;

      public LuaConfig(string scriptFile, string tableName) : base(scriptFile) {
         _tableName = tableName;
      }

      public T Load() {
         var table = GetGlobalTable(_tableName);
         return (T)MapTableToObject(typeof(T), table)!;
      }

      private object? MapTableToObject(Type targetType, Table table) {
         var result = Activator.CreateInstance(targetType);

         foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanWrite)
               continue;

            var key = prop.Name;
            var val = table.Get(key);

            if (val.IsNil())
               continue;

            object? converted = null;

            if (prop.PropertyType == typeof(string))
               converted = val.CastToString();
            else if (prop.PropertyType == typeof(int))
               converted = (int)(val.CastToNumber() ?? 0);
            else if (prop.PropertyType == typeof(double))
               converted = val.CastToNumber();
            else if (prop.PropertyType == typeof(bool))
               converted = val.CastToBool();
            else if (prop.PropertyType.IsEnum && val.Type == DataType.String)
               converted = Enum.Parse(prop.PropertyType, val.String, true);
            else if (prop.PropertyType.IsEnum && val.Type == DataType.Number)
               converted = Enum.ToObject(prop.PropertyType, (int)val.Number);
            else if (val.Type == DataType.Table) {
               // Rekursiv in Unterobjekt mappen
               converted = MapTableToObject(prop.PropertyType, val.Table);
            }

            if (converted != null)
               prop.SetValue(result, converted);
         }

         return result;
      }
   }

}
