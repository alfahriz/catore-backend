namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakCommands
{
    Task FreezeStreak(Guid userId);
}
