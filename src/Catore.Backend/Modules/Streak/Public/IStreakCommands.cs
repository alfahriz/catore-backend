namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakCommands
{
    Task FreezeStreak(Guid userId);
    Task RecordDailyLog(Guid userId, DateOnly date);
}
