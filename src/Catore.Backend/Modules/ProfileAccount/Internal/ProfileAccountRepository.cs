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

    public async Task<ProfileAccount?> GetByUserId(Guid userId)
    {
        return await _db.Set<ProfileAccount>().FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<Guid>> GetAllActiveUserIds()
    {
        return await _db.Set<ProfileAccount>()
            .Where(p => !p.IsDeleted && !p.IsUpgraded)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    public async Task Add(ProfileAccount profile)
    {
        _db.Set<ProfileAccount>().Add(profile);
        await _db.SaveChangesAsync();
    }

    public async Task Update(ProfileAccount profile)
    {
        profile.ModifiedOn = DateTime.UtcNow;
        _db.Set<ProfileAccount>().Update(profile);
        await _db.SaveChangesAsync();
    }
}
