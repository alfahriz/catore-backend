namespace Catore.Backend.Modules.Freeze.Public;

public interface IFreezeCommands
{
    Task<bool> ConsumeStreakFreeze(Guid userId);
    Task<bool> ConsumeWipeFreeze(Guid userId);
}
