// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Net.Sockets;
using KUpdater.Core.Attributes;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Security;
using MoonSharp.Interpreter;


namespace KUpdater.Scripting.Api;


[ExposeToLua("Website")]
public static class WebApi {
    private const string Capability = "Website.Open";
    public static LuaResult Open(string url) {
        if (string.IsNullOrWhiteSpace(url))
            return LuaResult.Fail("empty url");
        if (!LuaPolicy.IsAllowed(Capability))
            return LuaResult.Denied("Website.Open not permitted");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) ||
            (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
            return LuaResult.Fail("invalid url");

        try {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            return LuaResult.Success();
        }
        catch (Exception ex) {
            LuaDiagnostics.Error("Website.Open failed", ex);
            return LuaResult.Fail(ex.Message);
        }
    }
}




[ExposeToLua("ServerApi")]
public static class LuaServerApi {
    public static bool IsServerOnline(string ip, int port, int timeoutMs = 2000) {
        try {
            using var client = new TcpClient();
            var result = client.BeginConnect(ip, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(timeoutMs);
            return success && client.Connected;
        }
        catch {
            return false;
        }
    }

    public static DynValue StatusOf(string ip, int port) {
        bool online = IsServerOnline(ip, port);
        return DynValue.NewBoolean(online);
    }
}
