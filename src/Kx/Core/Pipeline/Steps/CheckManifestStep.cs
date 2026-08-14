// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Localization;
using Kx.Sdk.Events;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(20)]
public class CheckManifestStep : IUpdateStep {
    public string Name => "CheckManifest";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        if (!await HasManifestDifferencesAsync(ctx, ct)) {
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpToDateGeneric)));
            throw new OperationCanceledException("No update required");
        }

        eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpdateRequiredGeneric)));
        eventManager.NotifyAll(new UpdateRequired());

        await Task.CompletedTask;
    }

    private static async Task<bool> HasManifestDifferencesAsync(UpdateContext ctx, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(ctx);

        foreach (var file in ctx.Metadata.Files ?? []) {
            ct.ThrowIfCancellationRequested();
            var localPath = Path.Combine(ctx.RootDirectory, file.Path);
            var localFile = new FileInfo(localPath);

            if (!await localFile.VerifySha256Async(file.Sha256, ct).ConfigureAwait(false))
                return true;
        }

        foreach (var deletedFile in ctx.Metadata.DeletedFiles ?? []) {
            ct.ThrowIfCancellationRequested();
            if (File.Exists(Path.Combine(ctx.RootDirectory, deletedFile)))
                return true;
        }

        return false;
    }

}
