using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.Streak.Internal;

internal class StreakRepository
{
    private readonly AppDbContext _db;

    public StreakRepository(AppDbContext db)
    {
        _db = db;
    }

    // Dipakai job wipe-check biar soft-delete lintas-modul (Consumption, WeightTracking) +
    // reset streak/freeze state jalan dalam 1 transaction per user per run (Section 5.1).
    public async Task<IDbContextTransaction> BeginTransaction()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    public async Task<TStreak?> GetByUserId(long userId)
    {
        return await _db.Set<TStreak>().FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<List<TStreak>> GetAll()
    {
        return await _db.Set<TStreak>().ToListAsync();
    }

    public async Task<TStreak> Add(TStreak state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<TStreak>().Add(state);
        await _db.SaveChangesAsync();
        return state;
    }

    public async Task Update(TStreak state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<TStreak>().Update(state);
        await _db.SaveChangesAsync();
    }
}
