namespace Catore.Backend.Modules.ProfileAccount.Public;

public record ProfileAccountSummaryDto(
    string Timezone,
    decimal? GoalWeight,
    bool IsUpgraded,
    string GoalMode
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
    string GoalMode,
    decimal Tdee,
    decimal Bmi,
    string BmiCategory,
    IReadOnlyDictionary<string, decimal> CategoryLimits,
    DateTime? LastWipeOn
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

// Ganti mode (Cutting/Bulking/Maintain) — request TERPISAH dari UpdateProfileRequestDto krn
// alurnya beda total (trigger wipe, butuh konfirmasi eksplisit FE, bukan PATCH field biasa).
// FromMaintainSuggestion=true = auto-transisi dari saran pagar Maintain (WeightTrackingService.
// CheckMaintainRange) -- TANPA WIPE, streak lanjut jalan (requirement eksplisit, beda dari
// ganti mode manual dari Profile yang SELALU wipe).
public record ChangeGoalModeRequestDto(
    string NewGoalMode,
    decimal? GoalWeight,
    bool FromMaintainSuggestion = false
);

public record ChangeGoalModeResultDto(
    bool Success,
    string? ErrorMessage
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

// Null kalau user bukan mode Maintain atau profile gak lengkap. ExceedsRange=true berarti
// current TDEE tembus pagar [TDEE_saat_mulai_maintain-500, +350] -- SuggestedMode
// nunjuk arah saran pindah ("Cutting" kelebihan / "Bulking" kekurangan).
public record MaintainRangeCheckDto(
    bool ExceedsRange,
    string? SuggestedMode
);
