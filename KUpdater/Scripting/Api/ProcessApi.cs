// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Core.Attributes;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Security;

namespace KUpdater.Scripting.Api;


[ExposeToLua("ProcessInfo")]
public sealed class LuaProcessInfo {
    public string FileName { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public bool UseShellExecute { get; init; } = false;

    public LuaProcessInfo() { }

    public LuaProcessInfo(string fileName, string arguments = "", bool useShellExecute = false) {
        FileName = fileName ?? string.Empty;
        Arguments = arguments ?? string.Empty;
        UseShellExecute = useShellExecute;
    }
}


[ExposeToLua("Process")]
public static class ProcessApi {
    private const string CapabilityName = "Process.Start";

    public static LuaResult Start(LuaProcessInfo info) {
        if (info is null)
            return LuaResult.Fail("Missing process info");
        if (!LuaPolicy.IsAllowed(CapabilityName))
            return LuaResult.Denied($"DENIED: {CapabilityName} not permitted");
        if (string.IsNullOrWhiteSpace(info.FileName))
            return LuaResult.Fail("Empty filename");

        try {
            var file = Path.IsPathRooted(info.FileName)
                ? info.FileName
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, info.FileName);

            if (!LuaPathGuard.IsUnderAllowedRoot(file))
                return LuaResult.Fail("Path not allowed");

            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = info.Arguments ?? string.Empty,
                UseShellExecute = info.UseShellExecute
            };

            Process.Start(psi);
            LuaDiagnostics.Info($"Started process: {file} {info.Arguments}");
            return LuaResult.Success();
        }
        catch (Exception ex) {
            LuaDiagnostics.Error("LuaProcessApi.Start failed", ex);
            return LuaResult.Fail($"Failed to start process: {ex.Message}");
        }
    }
}
