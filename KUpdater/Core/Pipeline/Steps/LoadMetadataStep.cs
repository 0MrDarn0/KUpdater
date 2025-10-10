// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Json;
using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using SniffKit.Core;

namespace KUpdater.Core.Pipeline.Steps {
    [PipelineStep(10)]
    public class LoadMetadataStep : IUpdateStep {
        private readonly IUpdateSource _source;
        private readonly string _metadataUrl;
        private readonly string _changelogUrl;

        public string Name => "LoadMetadata";

        public LoadMetadataStep(IUpdateSource source, string baseUrl) {
            _source = source;
            _metadataUrl = baseUrl.EndsWith("/") ? baseUrl + "update.json" : baseUrl + "/update.json";
            _changelogUrl = baseUrl.EndsWith("/") ? baseUrl + "changelog.txt" : baseUrl + "/changelog.txt";
        }

        public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager) {
            // Status-Event
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.waiting")));

            // Metadaten laden
            var json = await _source.GetMetadataJsonAsync(_metadataUrl);
            ctx.Metadata = JsonSerializer.Deserialize<UpdateMetadata>(json)!;

            // Changelog laden
            var changelog = await _source.GetChangelogAsync(_changelogUrl);
            eventManager.NotifyAll(new ChangelogEvent(changelog));
        }
    }
}
