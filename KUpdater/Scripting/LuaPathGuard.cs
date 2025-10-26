// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Scripting;

public static class LuaPathGuard {
    private static readonly object Sync = new();
    private static string[] AllowedRoots = [AppDomain.CurrentDomain.BaseDirectory];

    public static void SetAllowedRoots(params string[] roots) {
        if (roots == null || roots.Length == 0)
            return;
        lock (Sync) {
            AllowedRoots = roots
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(NormalizeRoot)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public static void AddAllowedRoot(string root) {
        if (string.IsNullOrWhiteSpace(root))
            return;
        lock (Sync) {
            var list = AllowedRoots.ToList();
            var nr = NormalizeRoot(root);
            if (!list.Contains(nr, StringComparer.OrdinalIgnoreCase)) {
                list.Add(nr);
                AllowedRoots = list.ToArray();
            }
        }
    }

    public static bool IsUnderAllowedRoot(string path) {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        try {
            var full = Path.GetFullPath(path);
            lock (Sync) {
                return AllowedRoots.Any(root =>
                    full.StartsWith(root, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch { return false; }
    }

    private static string NormalizeRoot(string path) {
        var p = Path.GetFullPath(path);
        return p.EndsWith(Path.DirectorySeparatorChar) ? p : p + Path.DirectorySeparatorChar;
    }

    public static string[] GetAllowedRoots() {
        lock (Sync)
            return AllowedRoots.ToArray();
    }
}
