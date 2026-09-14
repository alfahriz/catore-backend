namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakCommands
{
    Task FreezeStreak(long userId);
    Task RecordDailyLog(long userId, DateOnly date);
}
