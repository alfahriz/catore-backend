namespace Catore.Backend.Modules.Freeze.Public;

public record FreezeTokenSummaryDto(
    int StreakFreezeCount,
    int WipeFreezeCount
);
