using Microsoft.Extensions.DependencyInjection;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.Param.Public;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.Consumption.Internal;

internal class ConsumptionService : IConsumptionQueries, IConsumptionCommands
{
    private const string DefaultDeficitCategory = "Mid";
    private const string DeficitCategoryParamType = "DEFICIT_CATEGORY";
    private const string MealTypeParamType = "MEAL_TYPE";
    private const string RecordFromParamType = "RECORD_FROM";
    private const string RecordFromLazyCreate = "Lazy Create";
    private const string RecordFromFreeze = "Freeze";

    private readonly ConsumptionRepository _repository;
    private readonly IProfileAccountQueries _profileQueries;
    private readonly IParamQueries _paramQueries;
    private readonly IServiceProvider _serviceProvider;

    public ConsumptionService(
        ConsumptionRepository repository,
        IProfileAccountQueries profileQueries,
        IParamQueries paramQueries,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _profileQueries = profileQueries;
        _paramQueries = paramQueries;
        _serviceProvider = serviceProvider;
    }

    // Resolve on-demand — StreakService juga depend balik ke IConsumptionQueries/Commands
    // (baca gap kalender, wipe data), circular kalau di-inject langsung di constructor.
    private IStreakCommands StreakCommands => _serviceProvider.GetRequiredService<IStreakCommands>();

    public async Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(long userId, DateOnly startDate, DateOnly endDate)
    {
        return await _repository.GetDailyTotalsForRange(userId, startDate, endDate);
    }

    public async Task<HashSet<DateOnly>> GetLoggedDates(long userId, DateOnly startDate, DateOnly endDate)
    {
        return await _repository.GetLoggedDates(userId, startDate, endDate);
    }

    public async Task WipeUserData(long userId, DateTime wipedAt, string wipeReason)
    {
        await _repository.WipeUserData(userId, wipedAt, wipeReason);
    }

    public async Task MarkDayFrozen(long userId, DateOnly date)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            if (effectiveLimit is null) return;

            var deficitCategoryPk = await _paramQueries.ResolvePk(DeficitCategoryParamType, DefaultDeficitCategory);
            var recordFromPk = await _paramQueries.ResolvePk(RecordFromParamType, RecordFromFreeze);

            dailyRecord = await _repository.AddDailyRecord(new TDailyRecord
            {
                UserId = userId,
                RecordDate = date,
                CalorieCategory = deficitCategoryPk,
                PaToday = false,
                EffectiveTdee = effectiveLimit.Tdee,
                EffectiveLimit = effectiveLimit.Limit,
                CreatedVia = recordFromPk,
                IsFrozen = true
            });
            return;
        }

        dailyRecord.IsFrozen = true;
        await _repository.UpdateDailyRecord(dailyRecord);
    }

    public async Task<DailyRecordDto> GetOrCreateDailyRecord(long userId, DateOnly date)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            var deficitCategoryPk = await _paramQueries.ResolvePk(DeficitCategoryParamType, DefaultDeficitCategory);
            var recordFromPk = await _paramQueries.ResolvePk(RecordFromParamType, RecordFromLazyCreate);

            dailyRecord = await _repository.AddDailyRecord(new TDailyRecord
            {
                UserId = userId,
                RecordDate = date,
                CalorieCategory = deficitCategoryPk,
                PaToday = false,
                EffectiveTdee = effectiveLimit?.Tdee ?? 0,
                EffectiveLimit = effectiveLimit?.Limit ?? 0,
                CreatedVia = recordFromPk,
                IsFrozen = false
            });
        }

        return await BuildDailyRecordDto(dailyRecord, userId, date);
    }

    public async Task<DailyRecordDto?> UpdateDailyRecord(long userId, DateOnly date, UpdateDailyRecordRequestDto request)
    {
        var dailyRecord = await _repository.GetDailyRecord(userId, date);
        if (dailyRecord is null) return null;

        var deficitCategoryName = request.CalorieCategory ?? await _paramQueries.ResolveName(dailyRecord.CalorieCategory) ?? DefaultDeficitCategory;
        var paToday = request.PaToday ?? dailyRecord.PaToday;

        var effectiveLimit = await _profileQueries.CalculateLimit(userId, deficitCategoryName, paToday);
        if (effectiveLimit is null) return null;

        dailyRecord.CalorieCategory = await _paramQueries.ResolvePk(DeficitCategoryParamType, deficitCategoryName);
        dailyRecord.PaToday = paToday;
        dailyRecord.EffectiveTdee = effectiveLimit.Tdee;
        dailyRecord.EffectiveLimit = effectiveLimit.Limit;
        await _repository.UpdateDailyRecord(dailyRecord);

        return await BuildDailyRecordDto(dailyRecord, userId, date);
    }

    private async Task<DailyRecordDto> BuildDailyRecordDto(TDailyRecord dailyRecord, long userId, DateOnly date)
    {
        var intakeSum = await _repository.GetIntakeSum(userId, date);
        var deficitCategoryName = await _paramQueries.ResolveName(dailyRecord.CalorieCategory) ?? DefaultDeficitCategory;
        return new DailyRecordDto(dailyRecord.RecordDate, deficitCategoryName, dailyRecord.PaToday, dailyRecord.EffectiveTdee, dailyRecord.EffectiveLimit, dailyRecord.IsFrozen, intakeSum);
    }

    // GLOBAL, bukan personal lagi — bank mconsumption dipakai bareng semua user.
    public async Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(string query, int page, int pageSize)
    {
        return await _repository.SearchAutocomplete(query, page, pageSize);
    }

    public async Task<IReadOnlyList<ConsumptionEntrySavedDto>> GetEntriesForDate(long userId, DateOnly date)
    {
        var entries = await _repository.GetEntriesForDate(userId, date);
        var result = new List<ConsumptionEntrySavedDto>();
        foreach (var (entry, consumption) in entries)
        {
            var mealTypeName = await _paramQueries.ResolveName(entry.MealType) ?? string.Empty;
            result.Add(new ConsumptionEntrySavedDto(entry.EntryPk, consumption.Name, consumption.Calories, mealTypeName, entry.EntryTimestamp));
        }
        return result;
    }

    public async Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(long userId)
    {
        var profile = await _profileQueries.GetProfileSummary(userId);
        if (profile is null) return Array.Empty<QuickAddItemDto>();

        // Sama pola guard StreakService.GetSignupToTodayRange (2026-09-16/17) — timezone kosong
        // (user baru abis Onboarding step 1, blm pernah refresh timezone) bikin FindSystemTimeZoneById
        // lempar TimeZoneNotFoundException 500. Timezone kosong = gak ada histori "kemarin" yg
        // relevan buat quick-add, aman return kosong.
        if (string.IsNullOrEmpty(profile.Timezone)) return Array.Empty<QuickAddItemDto>();

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
        var nowInTimezone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var yesterday = DateOnly.FromDateTime(nowInTimezone).AddDays(-1);

        return await _repository.GetQuickAdd(userId, yesterday);
    }

    public async Task<AddEntriesResultDto> AddEntries(long userId, AddEntriesRequestDto request)
    {
        if (request.Items.Count == 0)
        {
            return new AddEntriesResultDto(false, "At least one item is required", Array.Empty<ConsumptionEntrySavedDto>(), null);
        }

        var entryDate = DateOnly.FromDateTime(request.EntryTimestamp);
        var dailyRecord = await _repository.GetDailyRecord(userId, entryDate);
        if (dailyRecord is null)
        {
            var effectiveLimit = await _profileQueries.CalculateLimit(userId, DefaultDeficitCategory, paToday: false);
            if (effectiveLimit is null)
            {
                return new AddEntriesResultDto(false, "Profile not found", Array.Empty<ConsumptionEntrySavedDto>(), null);
            }

            var deficitCategoryPk = await _paramQueries.ResolvePk(DeficitCategoryParamType, DefaultDeficitCategory);
            var recordFromPk = await _paramQueries.ResolvePk(RecordFromParamType, RecordFromLazyCreate);

            dailyRecord = await _repository.AddDailyRecord(new TDailyRecord
            {
                UserId = userId,
                RecordDate = entryDate,
                CalorieCategory = deficitCategoryPk,
                PaToday = false,
                EffectiveTdee = effectiveLimit.Tdee,
                EffectiveLimit = effectiveLimit.Limit,
                CreatedVia = recordFromPk,
                IsFrozen = false
            });
        }

        var mealTypePk = await _paramQueries.ResolvePk(MealTypeParamType, request.MealType);

        var entries = new List<TConsumption>();
        foreach (var item in request.Items)
        {
            var consumptionId = await _repository.GetOrCreateConsumption(item.FoodName, item.Calories, userId);
            entries.Add(new TConsumption
            {
                UserId = userId,
                ConsumptionId = consumptionId,
                MealType = mealTypePk,
                EntryTimestamp = request.EntryTimestamp
            });
        }

        var saved = await _repository.AddEntries(entries);

        await StreakCommands.RecordDailyLog(userId, entryDate);

        // actualCalories WAJIB disinkronkan ulang tiap kali ada entry baru (lihat catatan
        // schema-curation soal cache ini) — dihitung ulang dari SUM tconsumption, bukan
        // increment manual, biar selalu akurat walau ada entry lama yg ke-soft-delete.
        var intakeSum = await _repository.GetIntakeSum(userId, entryDate);
        dailyRecord.ActualCalories = intakeSum;
        await _repository.UpdateDailyRecord(dailyRecord);

        var savedDtos = new List<ConsumptionEntrySavedDto>();
        foreach (var entry in saved)
        {
            var item = request.Items[saved.IndexOf(entry)];
            var mealTypeName = await _paramQueries.ResolveName(entry.MealType) ?? request.MealType;
            savedDtos.Add(new ConsumptionEntrySavedDto(entry.EntryPk, item.FoodName, item.Calories, mealTypeName, entry.EntryTimestamp));
        }

        var deficitCategoryName = await _paramQueries.ResolveName(dailyRecord.CalorieCategory) ?? DefaultDeficitCategory;

        return new AddEntriesResultDto(
            true,
            null,
            savedDtos,
            new DailyRecordDto(dailyRecord.RecordDate, deficitCategoryName, dailyRecord.PaToday, dailyRecord.EffectiveTdee, dailyRecord.EffectiveLimit, dailyRecord.IsFrozen, intakeSum)
        );
    }
}
