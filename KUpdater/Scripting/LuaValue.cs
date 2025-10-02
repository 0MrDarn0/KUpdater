using MoonSharp.Interpreter;

namespace KUpdater.Scripting {
   public readonly struct LuaValue<T> {
      public DynValue Raw { get; }
      public T? Value { get; }
      public bool IsValid { get; }

      public LuaValue(DynValue raw) {
         Raw = raw;

         try {
            Value = raw.ToObject<T>();
            IsValid = Value is not null;
         }
         catch {
            Value = default;
            IsValid = false;
         }
      }

      // Returns the value or a fallback if invalid
      public T GetOrDefault(T fallback) => IsValid ? Value! : fallback;

      // Tries to extract the value safely
      public bool TryGet(out T? val) {
         val = Value;
         return IsValid;
      }

      // Debug-friendly string output
      public override string ToString()
          => IsValid ? Value?.ToString() ?? "null" : $"[Invalid LuaValue<{typeof(T).Name}>]";
   }

}
