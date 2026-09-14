using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.Notification.Internal;

internal class NotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TNotification?> GetByUserId(long userId)
    {
        return await _db.Set<TNotification>().FirstOrDefaultAsync(n => n.UserId == userId);
    }

    public async Task UpsertFcmToken(long userId, string fcmToken)
    {
        var existing = await GetByUserId(userId);
        if (existing is not null)
        {
            existing.FcmToken = fcmToken;
            existing.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            _db.Set<TNotification>().Add(new TNotification
            {
                UserId = userId,
                FcmToken = fcmToken,
                ModifiedOn = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }
}
