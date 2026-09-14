namespace Catore.Backend.Modules.Consumption.Public;

public interface IConsumptionQueries
{
    Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(long userId, DateOnly startDate, DateOnly endDate);
    Task<HashSet<DateOnly>> GetLoggedDates(long userId, DateOnly startDate, DateOnly endDate);
    Task<DailyRecordDto> GetOrCreateDailyRecord(long userId, DateOnly date);

    // GLOBAL, BUKAN personal lagi -- bank mconsumption dipakai bareng semua user (lihat
    // catatan schema-curation soal keputusan ini). userId dihapus dari signature.
    Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(string query, int page, int pageSize);
    Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(long userId);
    Task<IReadOnlyList<ConsumptionEntrySavedDto>> GetEntriesForDate(long userId, DateOnly date);
}
