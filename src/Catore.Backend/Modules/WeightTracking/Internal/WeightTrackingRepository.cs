using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.WeightTracking.Internal;

internal class WeightTrackingRepository
{
    private readonly AppDbContext _db;

    public WeightTrackingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IDbContextTransaction> BeginTransaction()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    public async Task<List<TWeightLog>> GetByUserIdAndRange(long userId, DateOnly startDate, DateOnly endDate)
    {
        return await _db.Set<TWeightLog>()
            .Where(w => w.UserId == userId && !w.IsDeleted
                && w.CheckpointDate >= startDate && w.CheckpointDate <= endDate)
            .ToListAsync();
    }

    public async Task<TWeightLog?> GetByUserIdAndDate(long userId, DateOnly date)
    {
        return await _db.Set<TWeightLog>()
            .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted && w.CheckpointDate == date);
    }

    public async Task<TWeightLog> Add(TWeightLog entry)
    {
        entry.CreatedOn = DateTime.UtcNow;
        _db.Set<TWeightLog>().Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task Update(TWeightLog entry)
    {
        entry.ModifiedOn = DateTime.UtcNow;
        _db.Set<TWeightLog>().Update(entry);
        await _db.SaveChangesAsync();
    }

    public async Task WipeUserData(long userId, DateTime wipedAt)
    {
        var entries = await _db.Set<TWeightLog>()
            .Where(w => w.UserId == userId && !w.IsDeleted)
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.IsDeleted = true;
            entry.IsDeletedOn = wipedAt;
        }

        await _db.SaveChangesAsync();
    }
}
