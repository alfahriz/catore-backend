using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.WeightTracking.Internal;

internal class WeightTrackingRepository
{
    private readonly AppDbContext _db;

    public WeightTrackingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WeightLog>> GetByUserIdAndRange(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        return await _db.Set<WeightLog>()
            .Where(w => w.UserId == userId && !w.IsDeleted
                && DateOnly.FromDateTime(w.LoggedAt) >= startDate && DateOnly.FromDateTime(w.LoggedAt) <= endDate)
            .ToListAsync();
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
