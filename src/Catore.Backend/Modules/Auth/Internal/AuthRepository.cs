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

    public async Task<MUser?> GetByEmail(string email)
    {
        return await _db.Set<MUser>().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<MUser?> GetById(long userPk)
    {
        return await _db.Set<MUser>().FirstOrDefaultAsync(u => u.UserPk == userPk);
    }

    public async Task<bool> EmailExists(string email)
    {
        return await _db.Set<MUser>().AnyAsync(u => u.Email == email);
    }

    public async Task<MUser?> GetByResetToken(string resetToken)
    {
        return await _db.Set<MUser>().FirstOrDefaultAsync(u => u.ResetToken == resetToken);
    }

    public async Task<MUser?> GetByVerifyToken(string verifyToken)
    {
        return await _db.Set<MUser>().FirstOrDefaultAsync(u => u.EmailVerifiedToken == verifyToken);
    }

    public async Task<List<MUser>> GetExpiredUnverified(DateTime now)
    {
        return await _db.Set<MUser>()
            .Where(u => !u.IsEmailVerif && u.EmailVerifiedTokenExpiredAt != null && u.EmailVerifiedTokenExpiredAt < now)
            .ToListAsync();
    }

    public async Task Delete(MUser account)
    {
        _db.Set<MUser>().Remove(account);
        await _db.SaveChangesAsync();
    }

    public async Task Add(MUser account)
    {
        var now = DateTime.UtcNow;
        account.CreatedOn = now;
        account.ModifiedOn = now;
        _db.Set<MUser>().Add(account);
        await _db.SaveChangesAsync();
        account.CreatedBy = account.UserPk;
        await _db.SaveChangesAsync();
    }

    public async Task Update(MUser account)
    {
        account.ModifiedOn = DateTime.UtcNow;
        account.ModifiedBy = account.UserPk.ToString();
        _db.Set<MUser>().Update(account);
        await _db.SaveChangesAsync();
    }
}
