// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PipelineStepAttribute(int order, string? name = null) : Attribute {
    public int Order { get; } = order;
    public string? Name { get; } = name;
}
