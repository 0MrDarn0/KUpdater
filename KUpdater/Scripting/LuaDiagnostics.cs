// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace KUpdater.Scripting;


public static class LuaDiagnostics {
    public static void Info(string message) {
        if (message != null)
            Debug.WriteLine($"[Lua][INFO] {message}");
    }

    public static void Warn(string message) {
        if (message != null)
            Debug.WriteLine($"[Lua][WARN] {message}");
    }

    public static void Error(string message, Exception? ex = null) {
        Debug.WriteLine($"[Lua][ERROR] {message ?? ex?.Message ?? "<null>"}");
        if (ex != null)
            Debug.WriteLine(ex.ToString());
    }
}
