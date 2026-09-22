using Catore.Backend.Modules.Freeze.Public;
using Catore.Backend.Modules.Notification.Public;

namespace Catore.Backend.Modules.Freeze.Internal;

internal class FreezeService : IFreezeQueries, IFreezeCommands
{
    private const int MaxTokens = 2;
    private const int StreakFreezeThresholdDays = 30;
    private const int WipeFreezeThresholdDays = 60;

    private readonly FreezeRepository _repository;
    private readonly INotificationSender _notificationSender;

    public FreezeService(FreezeRepository repository, INotificationSender notificationSender)
    {
        _repository = repository;
        _notificationSender = notificationSender;
    }

    public async Task<TFreeze> GetOrCreate(long userId)
    {
        var state = await _repository.GetByUserId(userId);
        if (state is not null) return state;

        var newState = new TFreeze
        {
            UserId = userId,
            StreakFreezeCount = 0,
            WipeFreezeCount = 0,
            DaysSinceLastStreakFreeze = 0,
            DaysSinceLastWipeFreeze = 0
        };
        return await _repository.Add(newState);
    }

    public async Task<FreezeTokenSummaryDto?> GetAvailableTokens(long userId)
    {
        var state = await _repository.GetByUserId(userId);
        if (state is null) return null;
        return new FreezeTokenSummaryDto(state.StreakFreezeCount, state.WipeFreezeCount);
    }

    public async Task IncrementDaysSinceLastFreeze(long userId, DateOnly today)
    {
        var state = await GetOrCreate(userId);

        var gainedStreakFreeze = false;
        var gainedWipeFreeze = false;

        state.DaysSinceLastStreakFreeze += 1;
        if (state.DaysSinceLastStreakFreeze % StreakFreezeThresholdDays == 0 && state.StreakFreezeCount < MaxTokens)
        {
            state.StreakFreezeCount += 1;
            state.LastStreakFreezeGainedDate = today;
            gainedStreakFreeze = true;
        }

        state.DaysSinceLastWipeFreeze += 1;
        if (state.DaysSinceLastWipeFreeze % WipeFreezeThresholdDays == 0 && state.WipeFreezeCount < MaxTokens)
        {
            state.WipeFreezeCount += 1;
            state.LastWipeFreezeGainedDate = today;
            gainedWipeFreeze = true;
        }

        await _repository.Update(state);

        if (gainedStreakFreeze) await _notificationSender.SendFreezeGainedNotif(userId, "streak");
        if (gainedWipeFreeze) await _notificationSender.SendFreezeGainedNotif(userId, "wipe");
    }

    public async Task<bool> ConsumeStreakFreeze(long userId, DateOnly today)
    {
        var state = await GetOrCreate(userId);

        if (state.StreakFreezeCount <= 0) return false;
        if (state.LastStreakFreezeGainedDate == today) return false; // cooldown 1 hari

        state.StreakFreezeCount -= 1;
        state.DaysSinceLastStreakFreeze = 0;
        await _repository.Update(state);
        return true;
    }

    public async Task<bool> ConsumeWipeFreeze(long userId, DateOnly today)
    {
        var state = await GetOrCreate(userId);

        if (state.WipeFreezeCount <= 0) return false;
        if (state.LastWipeFreezeGainedDate == today) return false; // cooldown 1 hari

        state.WipeFreezeCount -= 1;
        state.DaysSinceLastWipeFreeze = 0;
        await _repository.Update(state);
        return true;
    }

    public async Task ResetAfterWipe(long userId, string wipeReason)
    {
        var state = await GetOrCreate(userId);
        state.StreakFreezeCount = 0;
        state.WipeFreezeCount = 0;
        state.DaysSinceLastStreakFreeze = 0;
        state.DaysSinceLastWipeFreeze = 0;
        state.LastStreakFreezeGainedDate = null;
        state.LastWipeFreezeGainedDate = null;
        state.WipeReason = wipeReason;
        await _repository.Update(state);
    }
}
