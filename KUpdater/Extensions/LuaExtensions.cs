// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

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

        // 🔹 Typensicheres Casten eines DynValue zu T mit fallback value
        public static T As<T>(this DynValue val, T fallback) {
            try {
                var result = val.ToObject<T>();
                return result is not null ? result : fallback;
            }
            catch {
                return fallback;
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

        public static bool IsTable(this DynValue val) => val.Type == DataType.Table;
        public static bool IsString(this DynValue val) => val.Type == DataType.String;
        public static bool IsNumber(this DynValue val) => val.Type == DataType.Number;
        public static bool IsFunction(this DynValue val) => val.Type == DataType.Function;
        public static bool IsUserData(this DynValue val) => val.Type == DataType.UserData;

        public static string? AsString(this DynValue val)
            => val.IsString() ? val.String : null;

        public static double? AsNumber(this DynValue val)
            => val.IsNumber() ? val.Number : null;

        public static Table? AsTable(this DynValue val)
            => val.IsTable() ? val.Table : null;

        public static Closure? AsFunction(this DynValue val)
            => val.IsFunction() ? val.Function : null;

        public static object? AsUserData(this DynValue val)
            => val.IsUserData() ? val.UserData.Object : null;

        public static Color AsColor(this DynValue val, Color fallback) {
            try {
                if (val.IsString())
                    return ColorTranslator.FromHtml(val.AsString()!);

                if (val.IsUserData() && val.AsUserData() is Color c)
                    return c;

                if (val.IsTable()) {
                    var t = val.AsTable()!;
                    int r = Clamp((int)(t.Get("r").AsNumber() ?? 0));
                    int g = Clamp((int)(t.Get("g").AsNumber() ?? 0));
                    int b = Clamp((int)(t.Get("b").AsNumber() ?? 0));
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }

            return fallback;
        }

        private static int Clamp(int value) => Math.Max(0, Math.Min(255, value));

    }
}
