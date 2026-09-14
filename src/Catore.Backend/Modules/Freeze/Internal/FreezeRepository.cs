using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.Freeze.Internal;

internal class FreezeRepository
{
    private readonly AppDbContext _db;

    public FreezeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TFreeze?> GetByUserId(long userId)
    {
        return await _db.Set<TFreeze>().FirstOrDefaultAsync(f => f.UserId == userId);
    }

    public async Task<TFreeze> Add(TFreeze state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<TFreeze>().Add(state);
        await _db.SaveChangesAsync();
        return state;
    }

    public async Task Update(TFreeze state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<TFreeze>().Update(state);
        await _db.SaveChangesAsync();
    }
}
