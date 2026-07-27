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

    public async Task<List<WeightLog>> GetByUserIdAndRange(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        return await _db.Set<WeightLog>()
            .Where(w => w.UserId == userId && !w.IsDeleted
                && DateOnly.FromDateTime(w.LoggedAt) >= startDate && DateOnly.FromDateTime(w.LoggedAt) <= endDate)
            .ToListAsync();
    }

    public async Task<WeightLog?> GetByUserIdAndDate(Guid userId, DateOnly date)
    {
        return await _db.Set<WeightLog>()
            .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted && DateOnly.FromDateTime(w.LoggedAt) == date);
    }

    public async Task<WeightLog> Add(WeightLog entry)
    {
        entry.CreatedOn = DateTime.UtcNow;
        _db.Set<WeightLog>().Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task Update(WeightLog entry)
    {
        entry.ModifiedOn = DateTime.UtcNow;
        _db.Set<WeightLog>().Update(entry);
        await _db.SaveChangesAsync();
    }

    public async Task WipeUserData(Guid userId, DateTime wipedAt)
    {
        var entries = await _db.Set<WeightLog>()
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
