// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using KUpdater.Core.Pipeline.Steps;

namespace KUpdater.Core.Pipeline {
    public class UpdaterPipelineRunner {
        private readonly IEventDispatcher _dispatcher;
        private readonly UpdatePipeline _pipeline;

        public UpdaterPipelineRunner(IEventDispatcher dispatcher, IUpdateSource source, string baseUrl, string rootDir) {
            _dispatcher = dispatcher;

            _pipeline = new UpdatePipeline()
                .AddStep(new LoadMetadataStep(source, baseUrl))
                .AddStep(new CheckVersionStep(rootDir))
                .AddStep(new DownloadAndExtractStep(source))
                .AddStep(new SaveVersionStep(rootDir));
        }

        public async Task RunAsync(string rootDir) {
            var ctx = new UpdateContext(rootDir);

            try {
                await _pipeline.RunAsync(ctx, _dispatcher);
            }
            catch (OperationCanceledException) {
                // Kein Update nötig → still ok
            }
            catch (Exception ex) {
                _dispatcher.Publish(new StatusEvent(
                    Localization.Translate("status.update_failed", ex.Message)
                ));
            }
        }
    }
}
