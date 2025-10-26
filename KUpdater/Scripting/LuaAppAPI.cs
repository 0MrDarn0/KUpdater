// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;

namespace KUpdater.Scripting;

[ExposeToLua("Application")]
public static class LuaAppAPI {
    public static void Exit() => Application.Exit();
}
