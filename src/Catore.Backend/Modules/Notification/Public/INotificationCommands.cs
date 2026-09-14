namespace Catore.Backend.Modules.Notification.Public;

public interface INotificationCommands
{
    Task UpdateFcmToken(long userId, string fcmToken);
}
