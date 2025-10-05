// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;

namespace KUpdater.Core.Event {
    public interface IEventDispatcher {
        void Publish<T>(T message);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }

    public class EventDispatcher : IEventDispatcher {
        // Thread-sichere Map: Eventtyp → Liste von Handlern
        private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
        private readonly object _lock = new();

        public void Publish<T>(T message) {
            if (_subscribers.TryGetValue(typeof(T), out var handlers)) {
                // Kopie erstellen, um gleichzeitige Änderungen zu vermeiden
                Delegate[] snapshot;
                lock (_lock) {
                    snapshot = handlers.ToArray();
                }

                foreach (var handler in snapshot.Cast<Action<T>>()) {
                    try {
                        handler(message);
                    }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"Event handler for {typeof(T).Name} threw: {ex}");
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> handler) {
            lock (_lock) {
                var list = _subscribers.GetOrAdd(typeof(T), _ => []);
                list.Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) {
            lock (_lock) {
                if (_subscribers.TryGetValue(typeof(T), out var list)) {
                    list.Remove(handler);
                    if (list.Count == 0)
                        _subscribers.TryRemove(typeof(T), out _);
                }
            }
        }

    }
}
