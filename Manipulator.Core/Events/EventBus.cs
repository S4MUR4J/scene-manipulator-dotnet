namespace Manipulator.Core.Events;

/// <summary>
/// Decoupled publish/subscribe bus for scene events.
/// Handlers are registered per event type and invoked when a matching event is published.
/// </summary>
/// <remarks>
/// Subscriptions are stored as (Type, Delegate) pairs rather than typed dictionaries so that
/// a single list scan can match both exact types and subtypes via <see cref="Type.IsInstanceOfType"/>.
/// Handler exceptions are silently swallowed to prevent one broken subscriber from stopping
/// delivery to the remaining subscribers.
/// </remarks>
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
