// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline {
    public interface IUpdateStep {
        string Name { get; }
        Task ExecuteAsync(UpdateContext context, IEventManager eventManager);
    }

    public class UpdateContext {
        public string RootDirectory { get; }
        public UpdateMetadata Metadata { get; set; } = new();
        public string CurrentVersion { get; set; } = "0.0.0";

        public UpdateContext(string rootDirectory) {
            RootDirectory = rootDirectory;
        }
    }

    public class UpdatePipeline {
        private readonly List<IUpdateStep> _steps = [];

        public UpdatePipeline AddStep(IUpdateStep step) {
            _steps.Add(step);
            return this;
        }

        public async Task RunAsync(UpdateContext context, IEventManager eventManager) {
            foreach (var step in _steps) {
                eventManager.NotifyAll(new UpdateStepStarted(step.Name));
                await step.ExecuteAsync(context, eventManager);
                eventManager.NotifyAll(new UpdateStepCompleted(step.Name));
            }

            eventManager.NotifyAll(new UpdatePipelineCompleted());
        }
    }
}
