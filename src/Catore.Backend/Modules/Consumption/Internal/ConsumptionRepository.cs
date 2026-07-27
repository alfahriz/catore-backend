using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;
using Catore.Backend.Modules.Consumption.Public;

namespace Catore.Backend.Modules.Consumption.Internal;

internal class ConsumptionRepository
{
    private readonly AppDbContext _db;

    public ConsumptionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DailyRecord?> GetDailyRecord(Guid userId, DateOnly date)
    {
        return await _db.Set<DailyRecord>()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.RecordDate == date && !d.IsDeleted);
    }

    public async Task<DailyRecord> AddDailyRecord(DailyRecord record)
    {
        record.ModifiedOn = DateTime.UtcNow;
        _db.Set<DailyRecord>().Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task UpdateDailyRecord(DailyRecord record)
    {
        record.ModifiedOn = DateTime.UtcNow;
        _db.Set<DailyRecord>().Update(record);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ConsumptionEntry>> AddEntries(List<ConsumptionEntry> entries)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            entry.CreatedOn = now;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Set<ConsumptionEntry>().AddRange(entries);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return entries;
    }

    public async Task<List<ConsumptionEntry>> GetEntriesForDate(Guid userId, DateOnly date)
    {
        return await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && e.EntryDate == date && !e.IsDeleted)
            .OrderBy(e => e.EntryTimestamp)
            .ToListAsync();
    }

    public async Task<int> GetIntakeSum(Guid userId, DateOnly date)
    {
        return await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && e.EntryDate == date && !e.IsDeleted)
            .SumAsync(e => (int?)e.Calories) ?? 0;
    }

    public async Task<HashSet<DateOnly>> GetLoggedDates(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var dates = await _db.Set<DailyRecord>()
            .Where(d => d.UserId == userId && d.RecordDate >= startDate && d.RecordDate <= endDate && !d.IsDeleted)
            .Select(d => d.RecordDate)
            .ToListAsync();
        return dates.ToHashSet();
    }

    public async Task WipeUserData(Guid userId, DateTime wipedAt)
    {
        var entries = await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .ToListAsync();
        foreach (var entry in entries)
        {
            entry.IsDeleted = true;
            entry.IsDeletedOn = wipedAt;
        }

        var records = await _db.Set<DailyRecord>()
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .ToListAsync();
        foreach (var record in records)
        {
            record.IsDeleted = true;
            record.IsDeletedOn = wipedAt;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(Guid userId, string query, int page, int pageSize)
    {
        var results = await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && !e.IsDeleted && EF.Functions.ILike(e.FoodName, $"%{query}%"))
            .GroupBy(e => new { e.FoodName, e.Calories })
            .OrderBy(g => g.Key.FoodName)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(g => new { g.Key.FoodName, g.Key.Calories })
            .ToListAsync();

        return results.Select(r => new AutocompleteItemDto(r.FoodName, r.Calories)).ToList();
    }

    public async Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(Guid userId, DateOnly yesterday)
    {
        var results = await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && !e.IsDeleted && e.EntryDate == yesterday)
            .GroupBy(e => new { e.FoodName, e.Calories, e.MealType })
            .Select(g => new { g.Key.FoodName, g.Key.Calories, g.Key.MealType })
            .ToListAsync();

        return results.Select(r => new QuickAddItemDto(r.FoodName, r.Calories, r.MealType)).ToList();
    }

    public async Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var records = await _db.Set<DailyRecord>()
            .Where(d => d.UserId == userId && d.RecordDate >= startDate && d.RecordDate <= endDate && !d.IsDeleted)
            .ToListAsync();

        var entries = await _db.Set<ConsumptionEntry>()
            .Where(e => e.UserId == userId && e.EntryDate >= startDate && e.EntryDate <= endDate && !e.IsDeleted)
            .GroupBy(e => e.EntryDate)
            .Select(g => new { Date = g.Key, Sum = g.Sum(e => e.Calories) })
            .ToDictionaryAsync(g => g.Date, g => g.Sum);

        return records.Select(r => new DailyTotalDto(
            r.RecordDate,
            r.EffectiveLimit,
            entries.GetValueOrDefault(r.RecordDate, 0),
            r.IsFrozen,
            entries.ContainsKey(r.RecordDate)
        )).ToList();
    }
}
