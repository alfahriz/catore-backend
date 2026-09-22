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

    public async Task WipeUserData(long userId, DateTime wipedAt, string wipeReason)
    {
        await _repository.WipeUserData(userId, wipedAt, wipeReason);
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
        // Goal-achieved arahnya tergantung goalMode: Cutting = turun ke goal (<=), Bulking =
        // naik ke goal (>=), Maintain = gak ada trigger achieved sama sekali (goalWeight di
        // Maintain cuma referensi visual "titik yg dijaga", bukan target yg "harus dicapai").
        var goalAchieved = !profile.IsUpgraded && profile.GoalWeight.HasValue && profile.GoalMode switch
        {
            "Bulking" => weightValue >= profile.GoalWeight.Value,
            "Maintain" => false,
            _ => weightValue <= profile.GoalWeight.Value // Cutting (default)
        };

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
        else if (profile.GoalMode == "Maintain")
        {
            // Pagar Maintain — cek SETELAH weight ke-update, gak masuk goalAchieved (Maintain
            // gak py trigger achieved). auto-transisi TANPA wipe kalau user setuju saran ini
            // ditangani terpisah lewat endpoint ChangeGoalMode biasa (streak lanjut jalan,
            // requirement eksplisit: auto-suggest BUKAN wipe, beda dari ganti mode manual).
            var rangeCheck = await _profileQueries.CheckMaintainRange(userId, weightValue);
            if (rangeCheck is { ExceedsRange: true, SuggestedMode: not null })
            {
                await _notificationSender.SendMaintainRangeExceededNotif(userId, rangeCheck.SuggestedMode);
            }
        }

        return new AddWeightLogResultDto(true, null, date, weightValue, goalAchieved);
    }
}
