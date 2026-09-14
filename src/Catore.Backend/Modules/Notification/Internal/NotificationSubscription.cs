namespace Catore.Backend.Modules.Notification.Internal;

internal class TNotification
{
    public long NotificationPk { get; set; }
    public long UserId { get; set; }
    public string? FcmToken { get; set; }
    public DateTime ModifiedOn { get; set; }
}
