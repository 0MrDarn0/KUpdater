// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)
using KUpdater.Core.Attributes;
using KUpdater.Scripting.Security;

namespace KUpdater.Scripting.Api;

[ExposeToLua("File")]
public static class FileAPI {
    /// <summary>
    /// Prüft, ob die Datei existiert und ob der Pfad unter einem erlaubten Root liegt.
    /// Gibt false zurück, wenn entweder nicht vorhanden oder Zugriff nicht erlaubt ist.
    /// </summary>
    public static bool Exists(string path) {
        try {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Auflösen relativer Pfade relativ zur BaseDirectory, wie deine bisherigen Lua-Strings es erwarten
            var full = Path.IsPathRooted(path)
                    ? Path.GetFullPath(path)
                    : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));

            if (!LuaPathGuard.IsUnderAllowedRoot(full)) {
                // Loggen, aber keinen UI-Block (UI entscheidet über Darstellung via LuaHost.Notify)
                LuaDiagnostics.Warn($"File.Exists blocked by path policy: {full}");
                return false;
            }

            return File.Exists(full);
        }
        catch (Exception ex) {
            LuaDiagnostics.Error("File.Exists failed", ex);
            return false;
        }
    }
}
