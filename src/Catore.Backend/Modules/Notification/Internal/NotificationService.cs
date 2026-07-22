using Catore.Backend.Modules.Notification.Public;

namespace Catore.Backend.Modules.Notification.Internal;

// STUB: belum kirim FCM asli, cuma log ke console.
// TODO: integrasi Firebase Admin SDK buat kirim push notification beneran.
internal class NotificationService : INotificationSender, INotificationCommands
{
    private readonly NotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(NotificationRepository repository, ILogger<NotificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task UpdateFcmToken(Guid userId, string fcmToken)
    {
        await _repository.UpsertFcmToken(userId, fcmToken);
    }

    public Task SendDailyReminder(Guid userId, bool includeSafetyFloorWarning)
    {
        _logger.LogInformation("[STUB] SendDailyReminder to {UserId}, safetyFloorWarning={Warning}", userId, includeSafetyFloorWarning);
        return Task.CompletedTask;
    }

    public Task SendGraceWindowCountdown(Guid userId, IReadOnlyList<DateOnly> outstandingDates, DateOnly oldestDeadline)
    {
        _logger.LogInformation("[STUB] SendGraceWindowCountdown to {UserId}, dates={Dates}, deadline={Deadline}", userId, outstandingDates, oldestDeadline);
        return Task.CompletedTask;
    }

    public Task SendWipeNotif(Guid userId)
    {
        _logger.LogInformation("[STUB] SendWipeNotif to {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task SendForceLogoutNotif(Guid userId)
    {
        _logger.LogInformation("[STUB] SendForceLogoutNotif to {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task SendFreezeUsedNotif(Guid userId, string freezeType, int remaining)
    {
        _logger.LogInformation("[STUB] SendFreezeUsedNotif to {UserId}, type={Type}, remaining={Remaining}", userId, freezeType, remaining);
        return Task.CompletedTask;
    }

    public Task SendFreezeGainedNotif(Guid userId, string freezeType)
    {
        _logger.LogInformation("[STUB] SendFreezeGainedNotif to {UserId}, type={Type}", userId, freezeType);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetRequestedNotif(Guid userId)
    {
        _logger.LogInformation("[STUB] SendPasswordResetRequestedNotif to {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task SendWeighInReminder(Guid userId)
    {
        _logger.LogInformation("[STUB] SendWeighInReminder to {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task SendGoalAchievedNotif(Guid userId, int frozenStreakCount)
    {
        _logger.LogInformation("[STUB] SendGoalAchievedNotif to {UserId}, streak={Streak}", userId, frozenStreakCount);
        return Task.CompletedTask;
    }
}
