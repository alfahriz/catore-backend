using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.Param.Internal;

namespace Catore.Backend.Modules.Consumption.Internal;

internal class ConsumptionRepository
{
    private readonly AppDbContext _db;

    public ConsumptionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TDailyRecord?> GetDailyRecord(long userId, DateOnly date)
    {
        return await _db.Set<TDailyRecord>()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.RecordDate == date && !d.IsDeleted);
    }

    public async Task<TDailyRecord> AddDailyRecord(TDailyRecord record)
    {
        record.ModifiedOn = DateTime.UtcNow;
        _db.Set<TDailyRecord>().Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task UpdateDailyRecord(TDailyRecord record)
    {
        record.ModifiedOn = DateTime.UtcNow;
        _db.Set<TDailyRecord>().Update(record);
        await _db.SaveChangesAsync();
    }

    // Dedup: cek dulu mconsumption yg name (trim+lowercase) + calories PERSIS sama sudah
    // ada -- kalau ada, pakai id itu (jangan bikin duplikat). Kalau belum ada, insert baru.
    public async Task<long> GetOrCreateConsumption(string name, int calories, long createdBy)
    {
        var trimmedLower = name.Trim().ToLowerInvariant();
        var existing = await _db.Set<MConsumption>()
            .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == trimmedLower && c.Calories == calories);
        if (existing is not null)
        {
            return existing.ConsumptionPk;
        }

        var created = new MConsumption
        {
            Name = name,
            Calories = calories,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _db.Set<MConsumption>().Add(created);
        await _db.SaveChangesAsync();
        return created.ConsumptionPk;
    }

    public async Task<List<TConsumption>> AddEntries(List<TConsumption> entries)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            entry.CreatedOn = now;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Set<TConsumption>().AddRange(entries);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return entries;
    }

    // Ambil entry + data mconsumption (nama/kalori) buat 1 tanggal, JOIN krn tconsumption
    // TIDAK simpan foodname/calories sendiri lagi (lihat MConsumption.cs).
    public async Task<List<(TConsumption Entry, MConsumption Consumption)>> GetEntriesForDate(long userId, DateOnly date)
    {
        var query = from e in _db.Set<TConsumption>()
                     join c in _db.Set<MConsumption>() on e.ConsumptionId equals c.ConsumptionPk
                     where e.UserId == userId && !e.IsDeleted && DateOnly.FromDateTime(e.EntryTimestamp) == date
                     orderby e.EntryTimestamp
                     select new { e, c };

        var results = await query.ToListAsync();
        return results.Select(r => (r.e, r.c)).ToList();
    }

    public async Task<int> GetIntakeSum(long userId, DateOnly date)
    {
        var query = from e in _db.Set<TConsumption>()
                     join c in _db.Set<MConsumption>() on e.ConsumptionId equals c.ConsumptionPk
                     where e.UserId == userId && !e.IsDeleted && DateOnly.FromDateTime(e.EntryTimestamp) == date
                     select c.Calories;

        return await query.SumAsync(calories => (int?)calories) ?? 0;
    }

    public async Task<HashSet<DateOnly>> GetLoggedDates(long userId, DateOnly startDate, DateOnly endDate)
    {
        var dates = await _db.Set<TDailyRecord>()
            .Where(d => d.UserId == userId && d.RecordDate >= startDate && d.RecordDate <= endDate && !d.IsDeleted)
            .Select(d => d.RecordDate)
            .ToListAsync();
        return dates.ToHashSet();
    }

    public async Task WipeUserData(long userId, DateTime wipedAt, string wipeReason)
    {
        var entries = await _db.Set<TConsumption>()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .ToListAsync();
        foreach (var entry in entries)
        {
            entry.IsDeleted = true;
            entry.IsDeletedOn = wipedAt;
            // tconsumption gak py kolom wipeReason (cuma tdailyrecord/tweightlog/tstreak/
            // tfreeze yg dikasih, lihat migration AddGoalModeAndWipeReason) -- alasan wipe
            // tetap bisa ditelusuri via JOIN ke tdailyrecord tanggal yg sama.
        }

        var records = await _db.Set<TDailyRecord>()
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .ToListAsync();
        foreach (var record in records)
        {
            record.IsDeleted = true;
            record.IsDeletedOn = wipedAt;
            record.WipeReason = wipeReason;
            // actualCalories WAJIB direset saat wipe, biar tetap sinkron sama tconsumption
            // yg baru disoft-delete di atas (lihat catatan schema-curation soal ini).
            record.ActualCalories = 0;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AutocompleteItemDto>> SearchAutocomplete(string query, int page, int pageSize)
    {
        var results = await _db.Set<MConsumption>()
            .Where(c => EF.Functions.ILike(c.Name, $"%{query}%"))
            .OrderBy(c => c.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Name, c.Calories })
            .ToListAsync();

        return results.Select(r => new AutocompleteItemDto(r.Name, r.Calories)).ToList();
    }

    // QuickAdd tetap PERSONAL (histori user itu sendiri), walau mconsumption-nya global --
    // JOIN tconsumption (personal) ke mconsumption (global) buat ambil detail item.
    public async Task<IReadOnlyList<QuickAddItemDto>> GetQuickAdd(long userId, DateOnly yesterday)
    {
        var query = from e in _db.Set<TConsumption>()
                     join c in _db.Set<MConsumption>() on e.ConsumptionId equals c.ConsumptionPk
                     join mt in _db.Set<MParam>() on e.MealType equals mt.ParamPk into mealTypeJoin
                     from mt in mealTypeJoin.DefaultIfEmpty()
                     where e.UserId == userId && !e.IsDeleted && DateOnly.FromDateTime(e.EntryTimestamp) == yesterday
                     group new { c, mt } by new { c.Name, c.Calories, MealTypeName = mt != null ? mt.Name : null } into g
                     select new { g.Key.Name, g.Key.Calories, g.Key.MealTypeName };

        var results = await query.ToListAsync();
        return results.Select(r => new QuickAddItemDto(r.Name, r.Calories, r.MealTypeName ?? string.Empty)).ToList();
    }

    public async Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(long userId, DateOnly startDate, DateOnly endDate)
    {
        var records = await _db.Set<TDailyRecord>()
            .Where(d => d.UserId == userId && d.RecordDate >= startDate && d.RecordDate <= endDate && !d.IsDeleted)
            .ToListAsync();

        var entriesQuery = from e in _db.Set<TConsumption>()
                            join c in _db.Set<MConsumption>() on e.ConsumptionId equals c.ConsumptionPk
                            where e.UserId == userId && !e.IsDeleted
                            select new { e.EntryTimestamp, c.Calories };

        var allEntries = await entriesQuery.ToListAsync();
        var entries = allEntries
            .Where(e => DateOnly.FromDateTime(e.EntryTimestamp) >= startDate && DateOnly.FromDateTime(e.EntryTimestamp) <= endDate)
            .GroupBy(e => DateOnly.FromDateTime(e.EntryTimestamp))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Calories));

        return records.Select(r => new DailyTotalDto(
            r.RecordDate,
            r.EffectiveLimit,
            entries.GetValueOrDefault(r.RecordDate, 0),
            r.IsFrozen,
            entries.ContainsKey(r.RecordDate)
        )).ToList();
    }
}
