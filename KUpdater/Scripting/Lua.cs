using System.Diagnostics;
using System.Reflection;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.Scripting {

   public abstract class Lua : IDisposable {
      protected Script _script;

      public Lua(string scriptFile) {
         string path = Path.Combine(AppContext.BaseDirectory, "kUpdater", "Lua", scriptFile);
         if (!File.Exists(path))
            throw new FileNotFoundException($"Lua script not found: {path}");

         _script = new Script();
         _script.Options.DebugPrint = s => Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Lua] >>> [{s}]");

         RegisterGlobals();

         _script.DoString(File.ReadAllText(path));

      }

      protected virtual void RegisterGlobals() {
         SetGlobal("__debug_globals", (Action)(() => {
            Debug.WriteLine("=== Lua Globals ===");
            foreach (var pair in _script.Globals.Pairs)
               Debug.WriteLine($"{pair.Key.ToPrintString()} : {pair.Value.Type}");
         }));
         SetGlobal("exe_directory", AppContext.BaseDirectory);
      }


      protected void SetGlobal(string name, object value)
          => _script.Globals[name] = DynValue.FromObject(_script, value);


      protected DynValue InvokeClosure(DynValue func, params object[] args)
          => func.Type == DataType.Function ? _script.Call(func, args) : DynValue.Nil;


      public DynValue Invoke(string functionName, params object[] args) {
         var func = _script.Globals.Get(functionName);
         return InvokeClosure(func, args);
      }


      public DynValue Invoke(DynValue func, params object[] args)
         => InvokeClosure(func, args);


      public LuaValue<T> Invoke<T>(string functionName, params object[] args)
         => new(Invoke(functionName, args));


      public LuaValue<T> Invoke<T>(DynValue func, params object[] args)
         => new(Invoke(func, args));


      public Table GetTableOrEmpty(string name) {
         var val = _script.Globals.Get(name);
         return val.Type == DataType.Table ? val.Table : new Table(_script);
      }


      public DynValue GetValue(string path) {
         var parts = path.Split('.');
         DynValue node = _script.Globals.Get(parts[0]);
         for (int i = 1; i < parts.Length; i++) {
            if (node.Type != DataType.Table)
               return DynValue.Nil;
            node = node.Table.Get(parts[i]);
         }
         return node;
      }


      public string? GetString(string path) {
         var val = GetValue(path);
         return val.Type == DataType.String ? val.String : null;
      }


      public void DumpTable(string path) {
         var val = GetValue(path);
         if (val.Type != DataType.Table) {
            Debug.WriteLine($"[Lua] {path} is not a table.");
            return;
         }

         Debug.WriteLine($"[Lua] Dumping table: {path}");
         foreach (var pair in val.Table.Pairs)
            Debug.WriteLine($"  {pair.Key.ToPrintString()} = {pair.Value}");
      }


      public void ExposeToLua<T>(string? globalName = null, T? instance = default) {
         var type = typeof(T);
         globalName ??= type.Name;

         UserData.RegisterType<T>();

         // 1) Enums: expose static and individual values
         if (type.IsEnum) {
            _script.Globals[globalName] = UserData.CreateStatic<T>();
            foreach (var name in Enum.GetNames(type)) {
               var value = Enum.Parse(type, name);
               _script.Globals[name] = UserData.Create(value);
            }
            return;
         }

         // 2) Instance: expose the instance only (userdata)
         if (instance is not null) {
            _script.Globals[globalName] = UserData.Create(instance);
            return;
         }

         // 3) Always expose statics
         _script.Globals[globalName] = UserData.CreateStatic<T>();

         // Do not expose a constructor for types like Color/SKColor (we want Color.White, etc.)
         bool exposeConstructor =
        type != typeof(Color) &&
        type != typeof(SKColor) &&
        !type.IsAbstract &&
        type.GetConstructors().Length > 0;

         if (!exposeConstructor)
            return;

         // 4) Constructor dispatcher without noisy exceptions
         _script.Globals[globalName] = DynValue.NewCallback((ctx, args) => {
            // Map MoonSharp DynValues to raw objects, but keep closures/tables intact for per-parameter matching.
            var rawArgs = args.GetArray()
            .Select(a =>
            {
               if (a.Type == DataType.Table)
                  return (object)a.Table;
               if (a.Type == DataType.Function)
                  return (object)a.Function; // keep Closure for targeted mapping
               if (a.Type == DataType.UserData)
                  return a.UserData.Object;  // unwrap .NET object
               return a.ToObject();
            })
            .ToArray();

            ConstructorInfo? chosen = null;
            object[]? finalArgs = null;

            foreach (var ctor in type.GetConstructors()) {
               var parms = ctor.GetParameters();
               int requiredCount = parms.Count(p => !p.HasDefaultValue);

               if (rawArgs.Length < requiredCount || rawArgs.Length > parms.Length)
                  continue;

               var tmp = new object?[parms.Length];
               bool ok = true;

               for (int i = 0; i < parms.Length; i++) {
                  var targetType = parms[i].ParameterType;

                  if (i < rawArgs.Length) {
                     var argVal = rawArgs[i];

                     if (!TryCoerce(argVal, targetType, out var coerced)) {
                        ok = false;
                        break;
                     }

                     tmp[i] = coerced;
                  } else {
                     if (parms[i].HasDefaultValue)
                        tmp[i] = parms[i].DefaultValue;
                     else {
                        ok = false;
                        break;
                     }
                  }
               }

               if (ok) {
                  chosen = ctor;
                  finalArgs = tmp!;
                  break;
               }
            }

            if (chosen == null)
               throw new ScriptRuntimeException($"No matching constructor found for {type.Name} with {rawArgs.Length} arguments.");

            var obj = chosen.Invoke(finalArgs!);
            return UserData.Create(obj);
         });

         // Local helper: targeted coercion without throwing exceptions
         bool TryCoerce(object? argVal, Type targetType, out object? result) {
            result = null;

            // Null handling
            if (argVal is null) {
               if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                  return false;
               result = null;
               return true;
            }

            var srcType = argVal.GetType();

            // Direct assignable
            if (targetType.IsAssignableFrom(srcType)) {
               result = argVal;
               return true;
            }

            // Lua Closure → Action
            if (targetType == typeof(Action) && argVal is Closure cb) {
               result = new Action(() => cb.Call());
               return true;
            }

            // Lua Closure → Func<Rectangle>
            if (targetType == typeof(Func<Rectangle>) && argVal is Closure boundsClosure) {
               result = new Func<Rectangle>(() => {
                  var ret = boundsClosure.Call();
                  if (ret.Type != DataType.Table)
                     throw new ScriptRuntimeException("bounds closure must return a table with x,y,width,height");

                  var t = ret.Table;
                  int x = (int)(t.Get("x").CastToNumber() ?? 0);
                  int y = (int)(t.Get("y").CastToNumber() ?? 0);
                  int w = (int)(t.Get("width").CastToNumber() ?? 0);
                  int h = (int)(t.Get("height").CastToNumber() ?? 0);

                  // Optional anchoring for negatives (keeps Lua simple)
                  var form = MainForm.Instance;
                  if (form != null) {
                     if (x < 0)
                        x = form.Width + x;
                     if (y < 0)
                        y = form.Height + y;
                     if (w < 0)
                        w = form.Width + w;
                     // h negative rarely used; add if needed
                  }

                  return new Rectangle(x, y, w, h);
               });
               return true;
            }

            // Enum: string name
            if (targetType.IsEnum && argVal is string s &&
                Enum.TryParse(targetType, s, true, out var enumVal)) {
               result = enumVal;
               return true;
            }

            // Enum: numeric
            if (targetType.IsEnum && argVal is double dnum) {
               result = Enum.ToObject(targetType, (int)dnum);
               return true;
            }

            // Numeric coercions from MoonSharp's double
            if (argVal is double d) {
               if (targetType == typeof(int)) { result = (int)d; return true; }
               if (targetType == typeof(float)) { result = (float)d; return true; }
               if (targetType == typeof(long)) { result = (long)d; return true; }
               if (targetType == typeof(short)) { result = (short)d; return true; }
               if (targetType == typeof(byte)) { result = (byte)d; return true; }
               if (targetType == typeof(decimal)) { result = (decimal)d; return true; }
               if (targetType == typeof(double)) { result = d; return true; }
            }

            // String → numeric (rare, but safe)
            if (argVal is string str) {
               if (targetType == typeof(int) && int.TryParse(str, out var i)) { result = i; return true; }
               if (targetType == typeof(double) && double.TryParse(str, out var dd)) { result = dd; return true; }
               if (targetType == typeof(float) && float.TryParse(str, out var ff)) { result = ff; return true; }
               if (targetType == typeof(long) && long.TryParse(str, out var ll)) { result = ll; return true; }
               if (targetType == typeof(decimal) && decimal.TryParse(str, out var mm)) { result = mm; return true; }
            }

            // Table → Rectangle (if constructor directly expects Rectangle)
            if (targetType == typeof(Rectangle) && argVal is Table tbl) {
               int x = (int)(tbl.Get("x").CastToNumber() ?? 0);
               int y = (int)(tbl.Get("y").CastToNumber() ?? 0);
               int w = (int)(tbl.Get("width").CastToNumber() ?? 0);
               int h = (int)(tbl.Get("height").CastToNumber() ?? 0);

               var form = MainForm.Instance;
               if (form != null) {
                  if (x < 0)
                     x = form.Width + x;
                  if (y < 0)
                     y = form.Height + y;
                  if (w < 0)
                     w = form.Width + w;
               }

               result = new Rectangle(x, y, w, h);
               return true;
            }

            // Last resort: try Convert.ChangeType only for simple primitives (avoid spamming exceptions)
            if (IsConvertiblePrimitive(srcType) && IsConvertiblePrimitive(targetType)) {
               try {
                  result = Convert.ChangeType(argVal, targetType);
                  return true;
               }
               catch {
                  // swallow; we'll return false
               }
            }

            return false;
         }

         static bool IsConvertiblePrimitive(Type t) {
            // Treat common primitives (including decimal, double) as convertible
            return t == typeof(bool) || t == typeof(byte) || t == typeof(short) ||
                   t == typeof(int) || t == typeof(long) || t == typeof(float) ||
                   t == typeof(double) || t == typeof(decimal) || t == typeof(char) ||
                   t == typeof(string);
         }
      }

      public virtual void Dispose() {
         _script?.Globals.Clear();
         _script = null!;
      }

   }
}
