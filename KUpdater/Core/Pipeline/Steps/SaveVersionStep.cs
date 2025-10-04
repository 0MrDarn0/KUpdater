// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline.Steps {
    public class SaveVersionStep : IUpdateStep {
        private readonly string _localVersionFile;
        public string Name => "SaveVersion";

        public SaveVersionStep(string rootDirectory) {
            _localVersionFile = Path.Combine(rootDirectory, "version.txt");
        }

        public async Task ExecuteAsync(UpdateContext ctx, IEventDispatcher dispatcher) {
            File.WriteAllText(_localVersionFile, ctx.Metadata.Version);

            dispatcher.Publish(new StatusEvent(
                Localization.Translate("status.update_applied")
            ));

            await Task.CompletedTask;
        }
    }
}
