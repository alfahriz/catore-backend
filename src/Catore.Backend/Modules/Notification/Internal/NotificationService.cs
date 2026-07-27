using FirebaseAdmin.Messaging;
using Catore.Backend.Modules.Notification.Public;

namespace Catore.Backend.Modules.Notification.Internal;

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

    // fcmtoken NULL/belum ada -> skip diam-diam, gak block flow (keputusan lama: fire-and-forget).
    private async Task SendPush(Guid userId, string title, string body, string? deepLink = null)
    {
        var subscription = await _repository.GetByUserId(userId);
        if (subscription is null || string.IsNullOrEmpty(subscription.FcmToken))
        {
            _logger.LogInformation("Skip push to {UserId} — no fcmtoken registered", userId);
            return;
        }

        var message = new Message
        {
            Token = subscription.FcmToken,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = deepLink is not null ? new Dictionary<string, string> { ["deepLink"] = deepLink } : null
        };

        try
        {
            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Push sent to {UserId}, messageId={MessageId}", userId, messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "Push failed to {UserId}: {ErrorCode}", userId, ex.MessagingErrorCode);
        }
    }

    public Task SendDailyReminder(Guid userId, bool includeSafetyFloorWarning)
    {
        var body = includeSafetyFloorWarning
            ? "Don't forget to log today — your limit is close to the safety floor."
            : "Don't forget to log your meals today.";
        return SendPush(userId, "Daily reminder", body);
    }

    public Task SendGraceWindowCountdown(Guid userId, IReadOnlyList<DateOnly> outstandingDates, DateOnly oldestDeadline)
    {
        var body = $"You have {outstandingDates.Count} day(s) missing. Log by {oldestDeadline:MMM d} to keep your streak.";
        return SendPush(userId, "Grace window active", body);
    }

    public Task SendWipeNotif(Guid userId)
    {
        return SendPush(userId, "Account wiped", "Your data has been reset after missing the grace window.");
    }

    public Task SendForceLogoutNotif(Guid userId)
    {
        return SendPush(userId, "Logged out", "Your account was signed in on another device.");
    }

    public Task SendFreezeUsedNotif(Guid userId, string freezeType, int remaining)
    {
        var label = freezeType == "streak" ? "Streak Freeze" : "Wipe Freeze";
        return SendPush(userId, $"{label} used", $"{label} was used automatically. {remaining} remaining.");
    }

    public Task SendFreezeGainedNotif(Guid userId, string freezeType)
    {
        var label = freezeType == "streak" ? "Streak Freeze" : "Wipe Freeze";
        return SendPush(userId, $"{label} earned", $"You've earned a new {label} token.");
    }

    public Task SendPasswordResetRequestedNotif(Guid userId)
    {
        return SendPush(userId, "Password reset requested", "Check your email to reset your password.");
    }

    public Task SendWeighInReminder(Guid userId)
    {
        return SendPush(userId, "Weigh-in reminder", "Don't forget to log your weight this week.");
    }

    public Task SendGoalAchievedNotif(Guid userId, int frozenStreakCount)
    {
        return SendPush(userId, "Goal achieved!", $"Congratulations, you've reached your goal weight. Your streak ({frozenStreakCount}) is now frozen.");
    }
}
