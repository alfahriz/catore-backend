namespace Catore.Backend.Modules.Notification.Internal;

internal class NotificationSubscription
{
    public Guid NotificationSubscriptionPk { get; set; }
    public Guid UserId { get; set; }
    public string? FcmToken { get; set; }
    public DateTime ModifiedOn { get; set; }
}
