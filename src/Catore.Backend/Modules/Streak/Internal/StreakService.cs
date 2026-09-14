using Catore.Backend.Modules.Auth.Public;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.Freeze.Public;
using Catore.Backend.Modules.Notification.Public;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.SharedKernel;
using Catore.Backend.Modules.Streak.Public;
using Catore.Backend.Modules.WeightTracking.Public;

namespace Catore.Backend.Modules.Streak.Internal;

internal class StreakService : IStreakQueries, IStreakCommands
{
    private readonly StreakRepository _repository;
    private readonly IAuthQueries _authQueries;
    private readonly IProfileAccountQueries _profileQueries;
    private readonly IProfileAccountCommands _profileCommands;
    private readonly IConsumptionQueries _consumptionQueries;
    private readonly IConsumptionCommands _consumptionCommands;
    private readonly IWeightTrackingCommands _weightTrackingCommands;
    private readonly IFreezeQueries _freezeQueries;
    private readonly IFreezeCommands _freezeCommands;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<StreakService> _logger;

    public StreakService(
        StreakRepository repository,
        IAuthQueries authQueries,
        IProfileAccountQueries profileQueries,
        IProfileAccountCommands profileCommands,
        IConsumptionQueries consumptionQueries,
        IConsumptionCommands consumptionCommands,
        IWeightTrackingCommands weightTrackingCommands,
        IFreezeQueries freezeQueries,
        IFreezeCommands freezeCommands,
        INotificationSender notificationSender,
        ILogger<StreakService> logger)
    {
        _repository = repository;
        _authQueries = authQueries;
        _profileQueries = profileQueries;
        _profileCommands = profileCommands;
        _consumptionQueries = consumptionQueries;
        _consumptionCommands = consumptionCommands;
        _weightTrackingCommands = weightTrackingCommands;
        _freezeQueries = freezeQueries;
        _freezeCommands = freezeCommands;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task<bool> HasActiveGraceWindow(long userId)
    {
        var missingDates = await GetMissingDates(userId);
        return missingDates.Count > 0;
    }

    private async Task<IReadOnlyList<MissingDateInfo>> GetMissingDates(long userId)
    {
        var account = await _authQueries.GetAccountInfo(userId);
        var profile = await _profileQueries.GetProfileSummary(userId);
        if (account is null || profile is null) return Array.Empty<MissingDateInfo>();

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
        var nowInTimezone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var today = DateOnly.FromDateTime(nowInTimezone);
        var signupDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(account.CreatedOn, timezone));

        var loggedDates = await _consumptionQueries.GetLoggedDates(userId, signupDate, today);
        return GraceWindowHelper.GetMissingDates(signupDate, today, loggedDates, timezone);
    }

    public async Task<StreakSummaryDto> GetStreakSummary(long userId)
    {
        var state = await GetOrCreate(userId);
        var tokens = await _freezeQueries.GetAvailableTokens(userId);

        return new StreakSummaryDto(
            state.CurrentStreak,
            state.LastLoggedDate,
            state.IsStreakFrozen,
            tokens?.StreakFreezeCount ?? 0,
            tokens?.WipeFreezeCount ?? 0);
    }

    public async Task<TStreak> GetOrCreate(long userId)
    {
        var state = await _repository.GetByUserId(userId);
        if (state is not null) return state;

        var newState = new TStreak
        {
            UserId = userId,
            CurrentStreak = 0,
            LastLoggedDate = null,
            IsStreakFrozen = false
        };
        return await _repository.Add(newState);
    }

    public async Task RecordDailyLog(long userId, DateOnly date)
    {
        var state = await GetOrCreate(userId);

        if (state.LastLoggedDate == date)
        {
            // Backfill/re-log tanggal yang sama (mis. batch kedua di hari yang sama) — no-op.
            return;
        }

        if (state.IsStreakFrozen)
        {
            // Akun Upgraded tanpa goal aktif (Section 5.7) — streak counter tidak bergerak sama sekali.
            return;
        }

        var oldLastLoggedDate = state.LastLoggedDate;
        var isBackfillOfPastDate = oldLastLoggedDate is not null && date < oldLastLoggedDate.Value;

        if (oldLastLoggedDate is null || date == oldLastLoggedDate.Value.AddDays(1))
        {
            state.CurrentStreak += 1;
        }
        else if (date > oldLastLoggedDate.Value.AddDays(1))
        {
            // Ada gap tanpa cover Streak Freeze (freeze coverage dievaluasi job wipe-check, bukan di sini) — reset.
            state.CurrentStreak = 1;
        }
        // isBackfillOfPastDate: backfill tanggal lampau setelah tanggal lebih baru sudah logged — counter tidak diutak-atik.

        if (date > (oldLastLoggedDate ?? DateOnly.MinValue))
        {
            state.LastLoggedDate = date;
        }

        await _repository.Update(state);

        // Cuma hari bersih "beneran" (bukan no-op re-log/backfill lampau) yang menghitung ke progress freeze token.
        if (!isBackfillOfPastDate)
        {
            await _freezeCommands.IncrementDaysSinceLastFreeze(userId, date);
        }
    }

    public async Task FreezeStreak(long userId)
    {
        var state = await GetOrCreate(userId);
        state.IsStreakFrozen = true;
        await _repository.Update(state);
    }

    // Dipanggil job wipe-check (WipeCheckJob) tiap jam, 1 user per panggilan.
    // Urutan eksekusi sesuai Section 5.1 & 5.6: evaluasi tanggal tertua yang expired ->
    // cek Streak Freeze dulu (cover -> Frozen, streak aman) -> gagal -> streak reset + lanjut cek Wipe Freeze
    // sebelum eksekusi wipe total.
    public async Task EvaluateWipeCheck(long userId, DateTime utcNow)
    {
        var missingDates = await GetMissingDates(userId);
        var oldestExpired = missingDates
            .Where(m => GraceWindowHelper.IsExpired(m, utcNow))
            .OrderBy(m => m.Date)
            .FirstOrDefault();

        if (oldestExpired is null) return;

        var today = oldestExpired.Date;
        string outcome;

        await using (var transaction = await _repository.BeginTransaction())
        {
            var state = await GetOrCreate(userId);

            var streakFreezeCovered = await _freezeCommands.ConsumeStreakFreeze(userId, today);
            if (streakFreezeCovered)
            {
                await _consumptionCommands.MarkDayFrozen(userId, today);
                outcome = "streak-freeze";
            }
            else
            {
                // Streak Freeze tidak tersedia/cukup -> streak reset, lanjut evaluasi wipe.
                state.CurrentStreak = 0;
                await _repository.Update(state);

                var wipeFreezeCovered = await _freezeCommands.ConsumeWipeFreeze(userId, today);
                if (wipeFreezeCovered)
                {
                    outcome = "wipe-freeze";
                }
                else
                {
                    // Tidak ada Wipe Freeze -> eksekusi wipe total (semua tabel data + reset streak/freeze).
                    await _consumptionCommands.WipeUserData(userId, utcNow);
                    await _weightTrackingCommands.WipeUserData(userId, utcNow);
                    await _profileCommands.MarkWiped(userId, utcNow);

                    state.CurrentStreak = 0;
                    state.LastLoggedDate = null;
                    state.IsStreakFrozen = false;
                    await _repository.Update(state);

                    await _freezeCommands.ResetAfterWipe(userId);
                    outcome = "wiped";
                }
            }

            await transaction.CommitAsync();
        }

        // Push notif dikirim setelah transaction commit — bukan bagian dari DB transaction.
        switch (outcome)
        {
            case "streak-freeze":
                var streakTokens = await _freezeQueries.GetAvailableTokens(userId);
                await _notificationSender.SendFreezeUsedNotif(userId, "streak", streakTokens?.StreakFreezeCount ?? 0);
                _logger.LogInformation("Streak Freeze auto-used for {UserId} on {Date}", userId, today);
                break;
            case "wipe-freeze":
                var wipeTokens = await _freezeQueries.GetAvailableTokens(userId);
                await _notificationSender.SendFreezeUsedNotif(userId, "wipe", wipeTokens?.WipeFreezeCount ?? 0);
                _logger.LogInformation("Wipe Freeze auto-used for {UserId}, wipe dibatalkan", userId);
                break;
            case "wiped":
                await _notificationSender.SendWipeNotif(userId);
                _logger.LogInformation("Account wiped for {UserId}", userId);
                break;
        }
    }
}
