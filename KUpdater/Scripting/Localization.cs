// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using MoonSharp.Interpreter;

namespace KUpdater.Scripting;

public static class Localization {
    private static Script? _script;

    public static void Initialize(Script script) {
        _script = script;
    }

    public static string Translate(string key, params object[] args) {
        if (_script == null)
            return key;

        var func = _script.Globals.Get("T");
        if (func.Type != DataType.Function && func.Type != DataType.ClrFunction)
            return key;

        var result = _script.Call(func, key);
        var raw = result.Type == DataType.String ? result.String : key;

        return args.Length > 0 ? string.Format(raw, args) : raw;
    }
}
