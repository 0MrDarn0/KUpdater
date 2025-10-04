// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Event {
    public interface IEventDispatcher {
        void Publish<T>(T message);
        void Subscribe<T>(Action<T> handler);
    }

    public class EventDispatcher : IEventDispatcher {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Publish<T>(T message) {
            if (_subscribers.TryGetValue(typeof(T), out var handlers)) {
                foreach (var handler in handlers.Cast<Action<T>>())
                    handler(message);
            }
        }

        public void Subscribe<T>(Action<T> handler) {
            if (!_subscribers.TryGetValue(typeof(T), out var handlers)) {
                handlers = [];
                _subscribers[typeof(T)] = handlers;
            }
            handlers.Add(handler);
        }
    }
}
