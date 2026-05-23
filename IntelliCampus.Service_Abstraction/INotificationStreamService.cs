using System.Threading.Channels;
using IntelliCampus.Shared.Dtos.Notification;

namespace IntelliCampus.Service_Abstraction;

public sealed record NotificationStreamSubscription(Guid ConnectionId, ChannelReader<NotificationDto> Reader);

public interface INotificationStreamService
{
    NotificationStreamSubscription Subscribe(int userId);
    void Publish(int userId, NotificationDto notification);
    void Unsubscribe(int userId, Guid connectionId);
}