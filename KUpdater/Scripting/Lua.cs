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
         // Kann von abgeleiteten Klassen überschrieben werden
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
   }
}
