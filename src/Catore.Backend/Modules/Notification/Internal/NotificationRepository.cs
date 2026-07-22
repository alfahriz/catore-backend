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

    public async Task<NotificationSubscription?> GetByUserId(Guid userId)
    {
        return await _db.Set<NotificationSubscription>().FirstOrDefaultAsync(n => n.UserId == userId);
    }

    public async Task UpsertFcmToken(Guid userId, string fcmToken)
    {
        var existing = await GetByUserId(userId);
        if (existing is not null)
        {
            existing.FcmToken = fcmToken;
            existing.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            _db.Set<NotificationSubscription>().Add(new NotificationSubscription
            {
                NotificationSubscriptionPk = Guid.NewGuid(),
                UserId = userId,
                FcmToken = fcmToken,
                ModifiedOn = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }
}
