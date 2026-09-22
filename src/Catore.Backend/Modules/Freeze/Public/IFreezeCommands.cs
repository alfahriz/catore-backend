namespace Catore.Backend.Modules.Freeze.Public;

public interface IFreezeCommands
{
    Task<bool> ConsumeStreakFreeze(long userId, DateOnly today);
    Task<bool> ConsumeWipeFreeze(long userId, DateOnly today);
    Task IncrementDaysSinceLastFreeze(long userId, DateOnly today);
    Task ResetAfterWipe(long userId, string wipeReason);
}
