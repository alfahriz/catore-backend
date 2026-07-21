namespace Catore.Backend.Modules.Notification.Public;

public interface INotificationSender
{
    Task SendDailyReminder(Guid userId, bool includeSafetyFloorWarning);
    Task SendGraceWindowCountdown(Guid userId, IReadOnlyList<DateOnly> outstandingDates, DateOnly oldestDeadline);
    Task SendWipeNotif(Guid userId);
    Task SendForceLogoutNotif(Guid userId);
    Task SendFreezeUsedNotif(Guid userId, string freezeType, int remaining);
    Task SendFreezeGainedNotif(Guid userId, string freezeType);
    Task SendPasswordResetRequestedNotif(Guid userId);
    Task SendWeighInReminder(Guid userId);
    Task SendGoalAchievedNotif(Guid userId, int frozenStreakCount);
}
