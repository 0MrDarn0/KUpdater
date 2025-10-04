// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline.Steps {
    [PipelineStep(15)]
    public class LoadChangelogStep : IUpdateStep {
        private readonly IUpdateSource _source;
        private readonly string _baseUrl;

        public LoadChangelogStep(IUpdateSource source, string baseUrl) {
            _source = source;
            _baseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        }

        public string Name => "LoadChangelog";

        public async Task ExecuteAsync(UpdateContext ctx, IEventDispatcher dispatcher) {
            try {
                string changelogUrl = _baseUrl + "changelog.txt";
                string changelog = await _source.GetChangelogAsync(changelogUrl);

                // Event feuern → landet in UIState
                dispatcher.Publish(new ChangelogEvent(changelog));
            }
            catch (Exception ex) {
                dispatcher.Publish(new StatusEvent(
                    Localization.Translate("status.changelog_failed", ex.Message)
                ));
            }
        }
    }
}
