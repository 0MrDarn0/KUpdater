// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)
using KUpdater.Core.Attributes;

namespace KUpdater.Scripting.Runtime;

[ExposeToLua("LuaResult")]
public sealed record LuaResult(bool Ok, string? Error = null, object? Payload = null) {
    public static LuaResult Success(object? payload = null) => new(true, null, payload);
    public static LuaResult Fail(string msg) => new(false, msg, null);
    public static LuaResult Denied(string reason) => new(false, reason, null);

    public bool IsDenied => !Ok && Error?.StartsWith("DENIED:", StringComparison.OrdinalIgnoreCase) == true;
    public string? ErrorOrNull => Error;
}
