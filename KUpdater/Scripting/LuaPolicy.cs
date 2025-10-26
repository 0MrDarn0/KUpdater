// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;

namespace KUpdater.Scripting;

public static class LuaPolicy {
    private static readonly ConcurrentDictionary<string, byte> Granted = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsAllowed(string capability) =>
        !string.IsNullOrWhiteSpace(capability) && Granted.ContainsKey(capability);

    public static void Grant(string capability) {
        if (!string.IsNullOrWhiteSpace(capability)) {
            Granted[capability] = 1;
            LuaDiagnostics.Info($"Capability granted: {capability}");
        }
    }

    public static void Revoke(string capability) {
        if (!string.IsNullOrWhiteSpace(capability)) {
            Granted.TryRemove(capability, out _);
            LuaDiagnostics.Info($"Capability revoked: {capability}");
        }
    }

    public static void Clear() {
        Granted.Clear();
        LuaDiagnostics.Info("All capabilities cleared");
    }

    public static IReadOnlyCollection<string> Snapshot() => (IReadOnlyCollection<string>)Granted.Keys;
}
