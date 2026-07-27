namespace Catore.Backend.Modules.WeightTracking.Public;

public record WeightEntryDto(
    DateOnly LoggedDate,
    decimal WeightValue
);

public record AddWeightLogRequestDto(
    decimal WeightValue
);

public record AddWeightLogResultDto(
    bool Success,
    string? ErrorMessage,
    DateOnly LoggedDate,
    decimal WeightValue,
    bool GoalAchieved
);
