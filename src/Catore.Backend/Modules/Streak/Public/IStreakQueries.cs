namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakQueries
{
    Task<bool> HasActiveGraceWindow(Guid userId);
    Task<StreakSummaryDto> GetStreakSummary(Guid userId);
}
