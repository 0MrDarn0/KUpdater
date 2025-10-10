// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using SniffKit.Core;

namespace KUpdater.Core.Pipeline {
    public class UpdaterPipelineRunner {
        private readonly IEventManager _eventManager;
        private readonly UpdatePipeline _pipeline;

        public UpdaterPipelineRunner(IEventManager eventManager, IUpdateSource source, string baseUrl, string rootDir) {
            _eventManager = eventManager;
            _pipeline = new UpdatePipeline();

            var stepTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IUpdateStep).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => new
                {
                    Type = t,
                    Attr = t.GetCustomAttribute<PipelineStepAttribute>()
                })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr!.Order);

            foreach (var stepInfo in stepTypes) {
                var ctor = stepInfo.Type.GetConstructors().First();
                var args = ctor.GetParameters().Select(p =>
                {
                    if (p.ParameterType == typeof(IUpdateSource))
                        return (object)source;
                    if (p.ParameterType == typeof(string) && p.Name!.Contains("base", StringComparison.OrdinalIgnoreCase))
                        return (object)baseUrl;
                    if (p.ParameterType == typeof(string) && p.Name!.Contains("root", StringComparison.OrdinalIgnoreCase))
                        return (object)rootDir;
                    throw new InvalidOperationException($"Unbekanntes ctor-Argument {p.Name} in {stepInfo.Type.Name}");
                }).ToArray();

                var step = (IUpdateStep)Activator.CreateInstance(stepInfo.Type, args)!;
                _pipeline.AddStep(step);
            }
        }

        public async Task RunAsync(string rootDir) {
            var ctx = new UpdateContext(rootDir);

            try {
                await _pipeline.RunAsync(ctx, _eventManager);
            }
            catch (OperationCanceledException) {
                // Kein Update nötig → still ok
            }
            catch (Exception ex) {
                _eventManager.NotifyAll(new StatusEvent(
                    Localization.Translate("status.update_failed", ex.Message)
                ));
            }
        }
    }
}
