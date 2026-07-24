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
