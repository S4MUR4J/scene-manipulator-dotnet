namespace Manipulator.Core.Events;

public class EventBus
{
    private readonly List<(Type EventType, Delegate Handler)> _subscriptions = [];

    public void Subscribe<T>(Action<T> handler)
        where T : ISceneEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Add((typeof(T), handler));
    }

    public void Unsubscribe<T>(Action<T> handler)
        where T : ISceneEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Remove((typeof(T), handler));
    }

    public void Publish(ISceneEvent sceneEvent)
    {
        ArgumentNullException.ThrowIfNull(sceneEvent);

        foreach (var (eventType, handler) in _subscriptions.ToList())
        {
            if (!eventType.IsInstanceOfType(sceneEvent))
                continue;

            try
            {
                handler.DynamicInvoke(sceneEvent);
            }
            catch
            {
                // Ignore.
            }
        }
    }
}
