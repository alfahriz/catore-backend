namespace Catore.Backend.Modules.Consumption.Public;

public record DailyTotalDto(
    DateOnly Date,
    decimal EffectiveLimit,
    int IntakeSum,
    bool IsFrozen,
    bool HasEntry
);

public record ConsumptionEntryItemDto(
    string FoodName,
    int Calories
);

public record AddEntriesRequestDto(
    DateOnly EntryDate,
    string MealType,
    DateTime EntryTimestamp,
    IReadOnlyList<ConsumptionEntryItemDto> Items
);

public record AddEntriesResultDto(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<ConsumptionEntrySavedDto> SavedEntries,
    DailyRecordDto? DailyRecord
);

public record ConsumptionEntrySavedDto(
    Guid ConsumptionEntryPk,
    string FoodName,
    int Calories,
    string MealType,
    DateTime EntryTimestamp
);

public record DailyRecordDto(
    DateOnly RecordDate,
    string DeficitCategory,
    bool PaToday,
    decimal EffectiveTdee,
    decimal EffectiveLimit,
    bool IsFrozen,
    int IntakeSum
);

public record UpdateDailyRecordRequestDto(
    string? DeficitCategory,
    bool? PaToday
);

public record AutocompleteItemDto(
    string FoodName,
    int Calories
);

public record QuickAddItemDto(
    string FoodName,
    int Calories,
    string MealType
);
