namespace Catore.Backend.Modules.ProfileAccount.Public;

public record ProfileAccountSummaryDto(
    string Timezone,
    decimal? GoalWeight,
    bool IsUpgraded
);

public record ProfileFullDto(
    decimal Height,
    decimal WeightCurrent,
    int Age,
    string Gender,
    string DisplayName,
    string BaselineActivityLevel,
    decimal? GoalWeight,
    bool GoalWeightIsManual,
    string MetricPreference,
    string Timezone,
    bool IsUpgraded,
    decimal Tdee,
    decimal Bmi,
    string BmiCategory,
    IReadOnlyDictionary<string, decimal> CategoryLimits
);

public record UpdateProfileRequestDto(
    decimal? Height,
    decimal? WeightCurrent,
    int? Age,
    string? Gender,
    string? DisplayName,
    decimal? GoalWeight,
    string? MetricPreference,
    string? Timezone
);

public record UpdateProfileResultDto(
    bool Success,
    string? ErrorMessage
);

public record ActivityAssessmentResultDto(
    bool Success,
    string ActivityLevel
);

public record TimezoneRefreshResultDto(
    bool Success,
    string? ErrorMessage
);

public record EffectiveLimitDto(
    decimal Tdee,
    decimal Limit
);
