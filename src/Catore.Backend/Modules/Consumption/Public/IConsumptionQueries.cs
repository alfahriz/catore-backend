namespace Catore.Backend.Modules.Consumption.Public;

public interface IConsumptionQueries
{
    Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(Guid userId, DateOnly startDate, DateOnly endDate);
    Task<HashSet<DateOnly>> GetLoggedDates(Guid userId, DateOnly startDate, DateOnly endDate);
    Task<DailyRecordDto> GetOrCreateDailyRecord(Guid userId, DateOnly date);
    Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(Guid userId, string query, int page, int pageSize);
    Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(Guid userId);
    Task<IReadOnlyList<ConsumptionEntrySavedDto>> GetEntriesForDate(Guid userId, DateOnly date);
}
