using Microsoft.Extensions.DependencyInjection;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Notification.Public;
using Catore.Backend.Modules.Streak.Public;
using Catore.Backend.Modules.WeightTracking.Public;

namespace Catore.Backend.Modules.WeightTracking.Internal;

internal class WeightTrackingService : IWeightTrackingQueries, IWeightTrackingCommands
{
    private readonly WeightTrackingRepository _repository;
    private readonly IProfileAccountQueries _profileQueries;
    private readonly INotificationSender _notificationSender;
    private readonly IServiceProvider _serviceProvider;

    public WeightTrackingService(
        WeightTrackingRepository repository,
        IProfileAccountQueries profileQueries,
        INotificationSender notificationSender,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _profileQueries = profileQueries;
        _notificationSender = notificationSender;
        _serviceProvider = serviceProvider;
    }

    // Resolve on-demand — StreakService juga depend balik ke IWeightTrackingCommands
    // (WipeUserData, dipanggil job wipe-check), circular kalau di-inject langsung di constructor.
    private IProfileAccountCommands ProfileCommands => _serviceProvider.GetRequiredService<IProfileAccountCommands>();
    private IStreakCommands StreakCommands => _serviceProvider.GetRequiredService<IStreakCommands>();
    private IStreakQueries StreakQueries => _serviceProvider.GetRequiredService<IStreakQueries>();

    public async Task<IReadOnlyList<WeightEntryDto>> GetWeightHistory(long userId, DateOnly startDate, DateOnly endDate)
    {
        var entries = await _repository.GetByUserIdAndRange(userId, startDate, endDate);
        return entries
            .Select(e => new WeightEntryDto(e.CheckpointDate, e.Weight))
            .ToList();
    }

    public async Task WipeUserData(long userId, DateTime wipedAt)
    {
        await _repository.WipeUserData(userId, wipedAt);
    }

    // Section 4.3: max 1 entry/hari, log ulang hari sama = update (edit), bukan insert baru.
    // Section 5.7: Goal Achieved trigger check dalam request yang sama, 1 DB transaction bareng insert/update weightlog.
    // CATATAN: checkpointDate di sini masih dipakai sbg "tanggal hari itu" (pola LAMA),
    // BELUM implementasi logic upsert mingguan (checkpoint Jumat + carry-forward) yg
    // direncanakan di schema-curation -- itu logic baru yg sengaja ditunda.
    public async Task<AddWeightLogResultDto> AddOrUpdateWeightLog(long userId, DateTime entryTimestamp, decimal weightValue)
    {
        var profile = await _profileQueries.GetProfileSummary(userId);
        if (profile is null)
        {
            return new AddWeightLogResultDto(false, "Profile not found", default, default, false);
        }

        var date = DateOnly.FromDateTime(entryTimestamp);
        var goalAchieved = !profile.IsUpgraded && profile.GoalWeight.HasValue && weightValue <= profile.GoalWeight.Value;

        await using (var transaction = await _repository.BeginTransaction())
        {
            var existing = await _repository.GetByUserIdAndDate(userId, date);
            if (existing is not null)
            {
                existing.Weight = weightValue;
                await _repository.Update(existing);
            }
            else
            {
                await _repository.Add(new TWeightLog
                {
                    UserId = userId,
                    Weight = weightValue,
                    CheckpointDate = date
                });
            }

            if (goalAchieved)
            {
                await ProfileCommands.SetUpgraded(userId);
                await StreakCommands.FreezeStreak(userId);
            }

            await transaction.CommitAsync();
        }

        if (goalAchieved)
        {
            var streakSummary = await StreakQueries.GetStreakSummary(userId);
            await _notificationSender.SendGoalAchievedNotif(userId, streakSummary.CurrentStreakCount);
        }

        return new AddWeightLogResultDto(true, null, date, weightValue, goalAchieved);
    }
}
