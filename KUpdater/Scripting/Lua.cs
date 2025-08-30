using MoonSharp.Interpreter;
using System.Diagnostics;

namespace KUpdater.Scripting {

   public abstract class Lua {
      protected Script _script;

      public Lua(string scriptFile) {
         string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", scriptFile);
         if (!File.Exists(path))
            throw new FileNotFoundException($"Lua script not found: {path}");

         _script = new Script();
         _script.Options.DebugPrint = s => Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Lua] >>> [{s}]");

         _script.DoString(File.ReadAllText(path));
         RegisterGlobals();
      }

      protected virtual void RegisterGlobals() {
         SetGlobal("__debug_globals", (Action)(() => {
            Console.WriteLine("=== Lua Globals Debug ===");
            foreach (var pair in _script.Globals.Pairs) {
               var key = pair.Key.ToPrintString();
               var val = pair.Value;
               Console.WriteLine($"{key} : {val.Type}");
            }
            Console.WriteLine("=========================");
         }));

         SetGlobal(LuaKeys.ExeDirectory, AppContext.BaseDirectory);
      }

      protected DynValue CallDynFunction(DynValue func, params object[] args)
         => func.Type == DataType.Function ? _script.Call(func, args) : DynValue.Nil;

      protected DynValue CallFunction(string functionName, params object[] args) {
         var func = _script.Globals.Get(functionName);
         return CallDynFunction(func, args);
      }

      protected Table GetGlobalTable(string name) =>
         _script.Globals.Get(name).Type == DataType.Table ? _script.Globals.Get(name).Table : new Table(_script);

      protected void SetGlobal(string name, object value) =>
         _script.Globals[name] = DynValue.FromObject(_script, value);

      public void ExposeToLua<T>(string? globalName = null, T? instance = default) {
         var type = typeof(T);
         globalName ??= type.Name;

         UserData.RegisterType<T>();

         if (type.IsEnum) {
            // Enum-Typ als statisches UserData
            _script.Globals[globalName] = UserData.CreateStatic<T>();

            // Alle Enum-Werte als einzelne Globals
            foreach (var name in Enum.GetNames(type)) {
               var value = Enum.Parse(type, name);
               _script.Globals[name] = UserData.Create(value);
            }
         } else if (instance is not null) {
            // Instanz als UserData
            _script.Globals[globalName] = UserData.Create(instance);
         } else {
            // Statische Member verfügbar machen
            _script.Globals[globalName] = UserData.CreateStatic<T>();

            // Konstruktor als callable Funktion in Lua
            _script.Globals[globalName] = DynValue.NewCallback((ctx, args) => {
               try {
                  var ctorArgs = args.GetArray()
                                   .Select(a => a.ToObject())
                                   .ToArray();

                  // Passenden Konstruktor suchen
                  var ctor = type.GetConstructors()
                               .FirstOrDefault(c =>
                               {
                                  var parms = c.GetParameters();
                                  if (parms.Length != ctorArgs.Length)
                                     return false;

                                  for (int i = 0; i < parms.Length; i++)
                                   {
                                     var targetType = parms[i].ParameterType;
                                     var argVal = ctorArgs[i];

                                     if (argVal == null)
                                       {
                                        if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                                           return false;
                                     }
                                       else if (!targetType.IsAssignableFrom(argVal.GetType()))
                                       {
                                        try
                                           {
                                           // Versuch, den Wert zu konvertieren
                                           ctorArgs[i] = Convert.ChangeType(argVal, targetType);
                                        }
                                        catch
                                           {
                                           // Enum-Konvertierung versuchen
                                           if (targetType.IsEnum && argVal is string s &&
                                                   Enum.TryParse(targetType, s, true, out var enumVal))
                                               {
                                              ctorArgs[i] = enumVal;
                                           }
                                               else if (targetType.IsEnum && argVal is double d)
                                               {
                                              ctorArgs[i] = Enum.ToObject(targetType, (int)d);
                                           }
                                               else
                                               {
                                              return false;
                                           }
                                        }
                                     }
                                  }
                                  return true;
                               });

                  if (ctor == null)
                     throw new ScriptRuntimeException($"No matching constructor found for {type.Name} with {ctorArgs.Length} arguments.");

                  var obj = ctor.Invoke(ctorArgs);
                  return UserData.Create(obj);
               }
               catch (Exception ex) {
                  throw new ScriptRuntimeException($"Error creating {type.Name}: {ex.Message}");
               }
            });
         }
      }

   }
}
