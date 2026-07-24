namespace Catore.Backend.Modules.Freeze.Public;

public interface IFreezeCommands
{
    Task<bool> ConsumeStreakFreeze(Guid userId, DateOnly today);
    Task<bool> ConsumeWipeFreeze(Guid userId, DateOnly today);
    Task IncrementDaysSinceLastFreeze(Guid userId, DateOnly today);
    Task ResetAfterWipe(Guid userId);
}
