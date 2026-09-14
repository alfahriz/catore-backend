using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class ProfileAccountRepository
{
    private readonly AppDbContext _db;

    public ProfileAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MProfile?> GetByUserId(long userId)
    {
        return await _db.Set<MProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<long>> GetAllActiveUserIds()
    {
        return await _db.Set<MProfile>()
            .Where(p => p.IsActive && !p.IsUpgraded)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    public async Task Add(MProfile profile)
    {
        _db.Set<MProfile>().Add(profile);
        await _db.SaveChangesAsync();
    }

    public async Task Update(MProfile profile)
    {
        profile.ModifiedOn = DateTime.UtcNow;
        _db.Set<MProfile>().Update(profile);
        await _db.SaveChangesAsync();
    }
}
