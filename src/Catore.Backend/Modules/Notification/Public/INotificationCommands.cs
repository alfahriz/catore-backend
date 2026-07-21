namespace Catore.Backend.Modules.Notification.Public;

public interface INotificationCommands
{
    Task UpdateFcmToken(Guid userId, string fcmToken);
}
