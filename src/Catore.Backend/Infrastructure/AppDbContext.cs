using Microsoft.EntityFrameworkCore;
using Catore.Backend.Modules.Auth.Internal;
using Catore.Backend.Modules.Profile.Internal;
using Catore.Backend.Modules.Consumption.Internal;
using Catore.Backend.Modules.WeightTracking.Internal;
using Catore.Backend.Modules.Streak.Internal;
using Catore.Backend.Modules.Freeze.Internal;
using Catore.Backend.Modules.Notification.Internal;

namespace Catore.Backend.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    internal DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    internal DbSet<Modules.Profile.Internal.Profile> Profiles => Set<Modules.Profile.Internal.Profile>();
    internal DbSet<ConsumptionEntry> ConsumptionEntries => Set<ConsumptionEntry>();
    internal DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    internal DbSet<WeightLog> WeightLogs => Set<WeightLog>();
    internal DbSet<StreakState> StreakStates => Set<StreakState>();
    internal DbSet<FreezeState> FreezeStates => Set<FreezeState>();
    internal DbSet<NotificationSubscription> NotificationSubscriptions => Set<NotificationSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("useraccount");
            entity.HasKey(e => e.UserAccountPk);
            entity.Property(e => e.UserAccountPk).HasColumnName("useraccountpk");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasColumnName("passwordhash").IsRequired();
            entity.Property(e => e.RefreshToken).HasColumnName("refreshtoken");
            entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refreshtokenexpiry");
            entity.Property(e => e.ActiveSessionId).HasColumnName("activesessionid");
            entity.Property(e => e.ResetToken).HasColumnName("resettoken");
            entity.Property(e => e.ResetTokenExpiry).HasColumnName("resettokenexpiry");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
        });

        modelBuilder.Entity<Modules.Profile.Internal.Profile>(entity =>
        {
            entity.ToTable("profile");
            entity.HasKey(e => e.ProfilePk);
            entity.Property(e => e.ProfilePk).HasColumnName("profilepk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.WeightCurrent).HasColumnName("weightcurrent");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.DisplayName).HasColumnName("displayname");
            entity.Property(e => e.BaselineActivityLevel).HasColumnName("baselineactivitylevel");
            entity.Property(e => e.GoalWeight).HasColumnName("goalweight");
            entity.Property(e => e.GoalWeightIsManual).HasColumnName("goalweightismanual");
            entity.Property(e => e.MetricPreference).HasColumnName("metricpreference");
            entity.Property(e => e.Timezone).HasColumnName("timezone");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted");
            entity.Property(e => e.IsUpgraded).HasColumnName("isupgraded");
        });

        modelBuilder.Entity<ConsumptionEntry>(entity =>
        {
            entity.ToTable("consumptionentry");
            entity.HasKey(e => e.ConsumptionEntryPk);
            entity.Property(e => e.ConsumptionEntryPk).HasColumnName("consumptionentrypk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.EntryDate).HasColumnName("entrydate");
            entity.Property(e => e.MealType).HasColumnName("mealtype");
            entity.Property(e => e.FoodName).HasColumnName("foodname").HasMaxLength(255);
            entity.Property(e => e.Calories).HasColumnName("calories");
            entity.Property(e => e.EntryTimestamp).HasColumnName("entrytimestamp");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isdeletedon");
        });

        modelBuilder.Entity<DailyRecord>(entity =>
        {
            entity.ToTable("dailyrecord");
            entity.HasKey(e => e.DailyRecordPk);
            entity.Property(e => e.DailyRecordPk).HasColumnName("dailyrecordpk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.RecordDate).HasColumnName("recorddate");
            entity.HasIndex(e => new { e.UserId, e.RecordDate }).IsUnique();
            entity.Property(e => e.DeficitCategory).HasColumnName("deficitcategory");
            entity.Property(e => e.PaToday).HasColumnName("patoday");
            entity.Property(e => e.EffectiveTdee).HasColumnName("effectivetdee");
            entity.Property(e => e.EffectiveLimit).HasColumnName("effectivelimit");
            entity.Property(e => e.CreatedVia).HasColumnName("createdvia");
            entity.Property(e => e.IsFrozen).HasColumnName("isfrozen");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isdeletedon");
        });

        modelBuilder.Entity<WeightLog>(entity =>
        {
            entity.ToTable("weightlog");
            entity.HasKey(e => e.WeightLogPk);
            entity.Property(e => e.WeightLogPk).HasColumnName("weightlogpk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.WeightValue).HasColumnName("weightvalue").HasColumnType("numeric(5,2)");
            entity.Property(e => e.LoggedAt).HasColumnName("loggedat");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isdeletedon");
        });

        modelBuilder.Entity<StreakState>(entity =>
        {
            entity.ToTable("streakstate");
            entity.HasKey(e => e.StreakStatePk);
            entity.Property(e => e.StreakStatePk).HasColumnName("streakstatepk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CurrentStreakCount).HasColumnName("currentstreakcount");
            entity.Property(e => e.LastLoggedDate).HasColumnName("lastloggeddate");
            entity.Property(e => e.StreakIsFrozen).HasColumnName("streakisfrozen");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
        });

        modelBuilder.Entity<FreezeState>(entity =>
        {
            entity.ToTable("freezestate");
            entity.HasKey(e => e.FreezeStatePk);
            entity.Property(e => e.FreezeStatePk).HasColumnName("freezestatepk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.StreakFreezeCount).HasColumnName("streakfreezecount");
            entity.Property(e => e.WipeFreezeCount).HasColumnName("wipefreezecount");
            entity.Property(e => e.DaysSinceLastStreakFreeze).HasColumnName("dayssincelaststreakfreeze");
            entity.Property(e => e.DaysSinceLastWipeFreeze).HasColumnName("dayssincelastwipefreeze");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
        });

        modelBuilder.Entity<NotificationSubscription>(entity =>
        {
            entity.ToTable("notificationsubscription");
            entity.HasKey(e => e.NotificationSubscriptionPk);
            entity.Property(e => e.NotificationSubscriptionPk).HasColumnName("notificationsubscriptionpk");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.FcmToken).HasColumnName("fcmtoken");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
        });
    }
}
