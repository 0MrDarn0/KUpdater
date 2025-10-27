// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;

namespace KUpdater.Scripting.Api;

[ExposeToLua("Application")]
public static class AppAPI {
    public static void Exit() => Application.Exit();
}
