namespace Catore.Backend.Modules.Profile.Public;

public record ProfileSummaryDto(
    string Timezone,
    decimal? GoalWeight,
    bool IsUpgraded
);
