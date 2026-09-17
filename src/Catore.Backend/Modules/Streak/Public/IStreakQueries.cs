namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakQueries
{
    Task<bool> HasActiveGraceWindow(long userId);
    Task<StreakSummaryDto> GetStreakSummary(long userId);
    Task<IReadOnlyList<MissingDateDto>> GetMissingDatesForDisplay(long userId);
    Task<IReadOnlyList<DateOnly>> GetUnfilledFrozenDays(long userId);
}
