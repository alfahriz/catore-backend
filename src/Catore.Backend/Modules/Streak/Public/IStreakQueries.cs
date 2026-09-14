namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakQueries
{
    Task<bool> HasActiveGraceWindow(long userId);
    Task<StreakSummaryDto> GetStreakSummary(long userId);
}
