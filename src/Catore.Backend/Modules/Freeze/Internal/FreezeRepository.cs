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

    public async Task<FreezeState?> GetByUserId(Guid userId)
    {
        return await _db.Set<FreezeState>().FirstOrDefaultAsync(f => f.UserId == userId);
    }

    public async Task<FreezeState> Add(FreezeState state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<FreezeState>().Add(state);
        await _db.SaveChangesAsync();
        return state;
    }

    public async Task Update(FreezeState state)
    {
        state.ModifiedOn = DateTime.UtcNow;
        _db.Set<FreezeState>().Update(state);
        await _db.SaveChangesAsync();
    }
}
