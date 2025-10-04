// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Json;
using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline.Steps {
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

        public async Task ExecuteAsync(UpdateContext ctx, IEventDispatcher dispatcher) {
            // Status-Event
            dispatcher.Publish(new StatusEvent(Localization.Translate("status.waiting")));

            // Metadaten laden
            var json = await _source.GetMetadataJsonAsync(_metadataUrl);
            ctx.Metadata = JsonSerializer.Deserialize<UpdateMetadata>(json)!;

            // Changelog laden
            var changelog = await _source.GetChangelogAsync(_changelogUrl);
            dispatcher.Publish(new ChangelogEvent(changelog));
        }
    }
}
