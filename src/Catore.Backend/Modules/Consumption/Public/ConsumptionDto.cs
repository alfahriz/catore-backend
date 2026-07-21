namespace Catore.Backend.Modules.Consumption.Public;

public record DailyTotalDto(
    DateOnly Date,
    decimal EffectiveLimit,
    int IntakeSum,
    bool IsFrozen,
    bool HasEntry
);
