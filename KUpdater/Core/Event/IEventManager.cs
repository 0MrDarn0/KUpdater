// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Event;

public interface IEventManager {
    public void Register<T>(Action<T> listener);
    public void Unregister<T>(Action<T> listener);
    public void NotifyAll<T>(T message);

    public void RegisterAsync<T>(AsyncAction<T> listener);
    public void UnregisterAsync<T>(AsyncAction<T> listener);
    public Task NotifyAllAsync<T>(T message);
}
