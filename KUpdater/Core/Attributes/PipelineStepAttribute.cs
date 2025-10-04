// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Attributes {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PipelineStepAttribute : Attribute {
        public int Order { get; }
        public string? Name { get; }

        public PipelineStepAttribute(int order, string? name = null) {
            Order = order;
            Name = name;
        }
    }
}
