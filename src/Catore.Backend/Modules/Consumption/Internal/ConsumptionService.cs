using Microsoft.Extensions.DependencyInjection;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.Consumption.Internal;

internal class ConsumptionService : IConsumptionQueries, IConsumptionCommands
{
    private const string DefaultDeficitCategory = "mid";

    private readonly ConsumptionRepository _repository;
    private readonly IProfileAccountQueries _profileQueries;
    private readonly IServiceProvider _serviceProvider;

    public ConsumptionService(ConsumptionRepository repository, IProfileAccountQueries profileQueries, IServiceProvider serviceProvider)
    {
        _repository = repository;
        _profileQueries = profileQueries;
        _serviceProvider = serviceProvider;
    }

    // Resolve on-demand — StreakService juga depend balik ke IConsumptionQueries/Commands
    // (baca gap kalender, wipe data), circular kalau di-inject langsung di constructor.
    private IStreakCommands StreakCommands => _serviceProvider.GetRequiredService<IStreakCommands>();

    public async Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        return await _repository.GetDailyTotalsForRange(userId, startDate, endDate);
    }

    public async Task<HashSet<DateOnly>> GetLoggedDates(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        return await _repository.GetLoggedDates(userId, startDate, endDate);
    }

    public async Task WipeUserData(Guid userId, DateTime wipedAt)
    {
        await _repository.WipeUserData(userId, wipedAt);
    }

    public async Task MarkDayFrozen(Guid userId, DateOnly date)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            if (effectiveLimit is null) return;

            dailyRecord = await _repository.AddDailyRecord(new DailyRecord
            {
                DailyRecordPk = Guid.NewGuid(),
                UserId = userId,
                RecordDate = date,
                DeficitCategory = DefaultDeficitCategory,
                PaToday = false,
                EffectiveTdee = effectiveLimit.Tdee,
                EffectiveLimit = effectiveLimit.Limit,
                CreatedVia = "freeze",
                IsFrozen = true
            });
            return;
        }

        dailyRecord.IsFrozen = true;
        await _repository.UpdateDailyRecord(dailyRecord);
    }

    public async Task<DailyRecordDto> GetOrCreateDailyRecord(Guid userId, DateOnly date)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            dailyRecord = await _repository.AddDailyRecord(new DailyRecord
            {
                DailyRecordPk = Guid.NewGuid(),
                UserId = userId,
                RecordDate = date,
                DeficitCategory = DefaultDeficitCategory,
                PaToday = false,
                EffectiveTdee = effectiveLimit?.Tdee ?? 0,
                EffectiveLimit = effectiveLimit?.Limit ?? 0,
                CreatedVia = "lazy-create",
                IsFrozen = false
            });
        }

        var intakeSum = await _repository.GetIntakeSum(userId, date);
        return new DailyRecordDto(dailyRecord.RecordDate, dailyRecord.DeficitCategory, dailyRecord.PaToday, dailyRecord.EffectiveTdee, dailyRecord.EffectiveLimit, dailyRecord.IsFrozen, intakeSum);
    }

    public async Task<DailyRecordDto?> UpdateDailyRecord(Guid userId, DateOnly date, UpdateDailyRecordRequestDto request)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null) return null;

        var deficitCategory = request.DeficitCategory ?? dailyRecord.DeficitCategory;
        var paToday = request.PaToday ?? dailyRecord.PaToday;

        var effectiveLimit = await _profileQueries.CalculateLimit(userId, deficitCategory, paToday);
        if (effectiveLimit is null) return null;

        dailyRecord.DeficitCategory = deficitCategory;
        dailyRecord.PaToday = paToday;
        dailyRecord.EffectiveTdee = effectiveLimit.Tdee;
        dailyRecord.EffectiveLimit = effectiveLimit.Limit;
        await _repository.UpdateDailyRecord(dailyRecord);

        var intakeSum = await _repository.GetIntakeSum(userId, date);
        return new DailyRecordDto(dailyRecord.RecordDate, dailyRecord.DeficitCategory, dailyRecord.PaToday, dailyRecord.EffectiveTdee, dailyRecord.EffectiveLimit, dailyRecord.IsFrozen, intakeSum);
    }

    public async Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(Guid userId, string query, int page, int pageSize)
    {
        return await _repository.SearchAutocomplete(userId, query, page, pageSize);
    }

    public async Task<IReadOnlyList<ConsumptionEntrySavedDto>> GetEntriesForDate(Guid userId, DateOnly date)
    {
        var entries = await _repository.GetEntriesForDate(userId, date);
        return entries.Select(e => new ConsumptionEntrySavedDto(e.ConsumptionEntryPk, e.FoodName, e.Calories, e.MealType, e.EntryTimestamp)).ToList();
    }

    public async Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(Guid userId)
    {
        var profile = await _profileQueries.GetProfileSummary(userId);
        if (profile is null) return Array.Empty<QuickAddItemDto>();

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
        var nowInTimezone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var yesterday = DateOnly.FromDateTime(nowInTimezone).AddDays(-1);

        return await _repository.GetQuickAdd(userId, yesterday);
    }

    public async Task<AddEntriesResultDto> AddEntries(Guid userId, AddEntriesRequestDto request)
    {
        if (request.Items.Count == 0)
        {
            return new AddEntriesResultDto(false, "At least one item is required", Array.Empty<ConsumptionEntrySavedDto>(), null);
        }

        var dailyRecord = await _repository.GetDailyRecord(userId, request.EntryDate);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            if (effectiveLimit is null)
            {
                return new AddEntriesResultDto(false, "Profile not found", Array.Empty<ConsumptionEntrySavedDto>(), null);
            }

            dailyRecord = await _repository.AddDailyRecord(new DailyRecord
            {
                DailyRecordPk = Guid.NewGuid(),
                UserId = userId,
                RecordDate = request.EntryDate,
                DeficitCategory = DefaultDeficitCategory,
                PaToday = false,
                EffectiveTdee = effectiveLimit.Tdee,
                EffectiveLimit = effectiveLimit.Limit,
                CreatedVia = "lazy-create",
                IsFrozen = false
            });
        }

        var entries = request.Items.Select(item => new ConsumptionEntry
        {
            ConsumptionEntryPk = Guid.NewGuid(),
            UserId = userId,
            EntryDate = request.EntryDate,
            MealType = request.MealType,
            FoodName = item.FoodName,
            Calories = item.Calories,
            EntryTimestamp = request.EntryTimestamp
        }).ToList();

        var saved = await _repository.AddEntries(entries);

        await StreakCommands.RecordDailyLog(userId, request.EntryDate);

        var intakeSum = await _repository.GetIntakeSum(userId, request.EntryDate);

        return new AddEntriesResultDto(
            true,
            null,
            saved.Select(e => new ConsumptionEntrySavedDto(e.ConsumptionEntryPk, e.FoodName, e.Calories, e.MealType, e.EntryTimestamp)).ToList(),
            new DailyRecordDto(dailyRecord.RecordDate, dailyRecord.DeficitCategory, dailyRecord.PaToday, dailyRecord.EffectiveTdee, dailyRecord.EffectiveLimit, dailyRecord.IsFrozen, intakeSum)
        );
    }
}
