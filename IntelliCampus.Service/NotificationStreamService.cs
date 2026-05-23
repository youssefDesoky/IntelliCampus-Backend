using System.Collections.Concurrent;
using System.Threading.Channels;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Notification;

namespace IntelliCampus.Service;

public class NotificationStreamService : INotificationStreamService
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Channel<NotificationDto>>> _connections = new();

    public NotificationStreamSubscription Subscribe(int userId)
    {
        var connectionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<NotificationDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<NotificationDto>>());
        userConnections[connectionId] = channel;

        return new NotificationStreamSubscription(connectionId, channel.Reader);
    }

    public void Publish(int userId, NotificationDto notification)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
            return;

        foreach (var channel in userConnections.Values)
        {
            channel.Writer.TryWrite(notification);
        }
    }

    public void Unsubscribe(int userId, Guid connectionId)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
            return;

        if (userConnections.TryRemove(connectionId, out var channel))
        {
            channel.Writer.TryComplete();
        }

        if (userConnections.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }
    }
}