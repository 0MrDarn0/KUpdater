// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Scripting.Security;

namespace KUpdater.Scripting.Runtime;

[ExposeToLua("Host")]
public static class LuaHost {
    public static event Action<string, string>? OnNotify;

    public static void Notify(string level, string message) {
        try {
            OnNotify?.Invoke(level ?? "Info", message ?? string.Empty);
            LuaDiagnostics.Info($"Notify: {level} - {message}");
        }
        catch (Exception ex) {
            LuaDiagnostics.Error("LuaHost.Notify failed", ex);
        }
    }
}
