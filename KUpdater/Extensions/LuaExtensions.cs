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

        public static Color AsColor(this DynValue val, Color fallback) {
            try {
                if (val.Type == DataType.String)
                    return ColorTranslator.FromHtml(val.String);

                if (val.Type == DataType.UserData && val.UserData.Object is Color c)
                    return c;

                if (val.Type == DataType.Table) {
                    var t = val.Table;
                    int r = Clamp((int)(t.Get("r").CastToNumber() ?? 0));
                    int g = Clamp((int)(t.Get("g").CastToNumber() ?? 0));
                    int b = Clamp((int)(t.Get("b").CastToNumber() ?? 0));
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }

            return fallback;
        }

        private static int Clamp(int value) => Math.Max(0, Math.Min(255, value));

    }
}
