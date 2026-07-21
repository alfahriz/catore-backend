namespace Catore.Backend.Modules.WeightTracking.Public;

public record WeightEntryDto(
    DateOnly LoggedDate,
    decimal WeightValue
);
