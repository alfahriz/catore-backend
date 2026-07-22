using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.Auth.Internal;

internal class AuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserAccount?> GetByEmail(string email)
    {
        return await _db.Set<UserAccount>().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserAccount?> GetById(Guid userAccountPk)
    {
        return await _db.Set<UserAccount>().FirstOrDefaultAsync(u => u.UserAccountPk == userAccountPk);
    }

    public async Task<bool> EmailExists(string email)
    {
        return await _db.Set<UserAccount>().AnyAsync(u => u.Email == email);
    }

    public async Task<UserAccount?> GetByResetToken(string resetToken)
    {
        return await _db.Set<UserAccount>().FirstOrDefaultAsync(u => u.ResetToken == resetToken);
    }

    public async Task Add(UserAccount account)
    {
        var now = DateTime.UtcNow;
        account.CreatedOn = now;
        account.ModifiedOn = now;
        _db.Set<UserAccount>().Add(account);
        await _db.SaveChangesAsync();
    }

    public async Task Update(UserAccount account)
    {
        account.ModifiedOn = DateTime.UtcNow;
        _db.Set<UserAccount>().Update(account);
        await _db.SaveChangesAsync();
    }
}
