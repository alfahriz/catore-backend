namespace Catore.Backend.Modules.Streak.Public;

public record StreakSummaryDto(
    int CurrentStreakCount,
    DateOnly? LastLoggedDate,
    bool StreakIsFrozen,
    int StreakFreezeCount,
    int WipeFreezeCount
);
