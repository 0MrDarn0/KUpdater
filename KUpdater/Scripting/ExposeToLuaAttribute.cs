// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Scripting {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    public sealed class ExposeToLuaAttribute : Attribute {
        public string? GlobalName { get; }
        public ExposeToLuaAttribute(string? globalName = null) => GlobalName = globalName;
    }
}
