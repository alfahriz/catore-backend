namespace Catore.Backend.Modules.Notification.Public;

public interface INotificationSender
{
    Task SendDailyReminder(long userId, bool includeSafetyFloorWarning);
    Task SendGraceWindowCountdown(long userId, IReadOnlyList<DateOnly> outstandingDates, DateOnly oldestDeadline);
    Task SendWipeNotif(long userId);
    Task SendForceLogoutNotif(long userId);
    Task SendFreezeUsedNotif(long userId, string freezeType, int remaining);
    Task SendFreezeGainedNotif(long userId, string freezeType);
    Task SendPasswordResetRequestedNotif(long userId);
    Task SendWeighInReminder(long userId);
    Task SendGoalAchievedNotif(long userId, int frozenStreakCount);
    // Maintain mode: current weight/TDEE tembus pagar [TDEE-500, TDEE+350] -- saran pindah
    // Cutting/Bulking. suggestedMode = "Cutting" (kelebihan) atau "Bulking" (kekurangan).
    Task SendMaintainRangeExceededNotif(long userId, string suggestedMode);
}
