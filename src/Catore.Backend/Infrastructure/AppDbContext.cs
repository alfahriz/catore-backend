using Microsoft.EntityFrameworkCore;
using Catore.Backend.Modules.Auth.Internal;
using Catore.Backend.Modules.Param.Internal;
using Catore.Backend.Modules.ProfileAccount.Internal;
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

    internal DbSet<MUser> MUsers => Set<MUser>();
    internal DbSet<MParam> MParams => Set<MParam>();
    internal DbSet<MParamNotif> MParamNotifs => Set<MParamNotif>();
    internal DbSet<MProfile> MProfiles => Set<MProfile>();
    internal DbSet<MConsumption> MConsumptions => Set<MConsumption>();
    internal DbSet<TConsumption> TConsumptions => Set<TConsumption>();
    internal DbSet<TDailyRecord> TDailyRecords => Set<TDailyRecord>();
    internal DbSet<TWeightLog> TWeightLogs => Set<TWeightLog>();
    internal DbSet<TStreak> TStreaks => Set<TStreak>();
    internal DbSet<TFreeze> TFreezes => Set<TFreeze>();
    internal DbSet<TNotification> TNotifications => Set<TNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MUser>(entity =>
        {
            entity.ToTable("muser");
            entity.HasKey(e => e.UserPk);
            entity.Property(e => e.UserPk).HasColumnName("userPk");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Password).HasColumnName("password").IsRequired();
            entity.Property(e => e.JwtRefreshToken).HasColumnName("jwtRefreshToken");
            entity.Property(e => e.JwtRefreshTokenExpiredAt).HasColumnName("jwtRefreshTokenExpiredAt");
            entity.Property(e => e.SessionId).HasColumnName("sessionId");
            entity.Property(e => e.ResetToken).HasColumnName("resetToken");
            entity.Property(e => e.ResetTokenExpiredAt).HasColumnName("resetTokenExpiredAt");
            entity.Property(e => e.IsEmailVerif).HasColumnName("isEmailVerif");
            entity.Property(e => e.EmailVerifiedToken).HasColumnName("emailVerifiedToken");
            entity.Property(e => e.EmailVerifiedTokenExpiredAt).HasColumnName("emailVerifiedTokenExpiredAt");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.Property(e => e.ModifiedBy).HasColumnName("modifiedBy");
        });

        modelBuilder.Entity<MParam>(entity =>
        {
            entity.ToTable("mparam");
            entity.HasKey(e => e.ParamPk);
            entity.Property(e => e.ParamPk).HasColumnName("paramPK");
            entity.Property(e => e.ParamType).HasColumnName("paramType").IsRequired();
            entity.HasIndex(e => e.ParamType);
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
        });

        modelBuilder.Entity<MParamNotif>(entity =>
        {
            entity.ToTable("mparamnotif");
            entity.HasKey(e => e.NotifPk);
            entity.Property(e => e.NotifPk).HasColumnName("notifPk");
            entity.Property(e => e.Key).HasColumnName("key").IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.Type).HasConstraintName("FK_mparamnotif_type");
        });

        modelBuilder.Entity<MProfile>(entity =>
        {
            entity.ToTable("mprofile");
            entity.HasKey(e => e.ProfilePk);
            entity.Property(e => e.ProfilePk).HasColumnName("profilePK");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.GoalWeight).HasColumnName("goalWeight");
            entity.Property(e => e.IsRecomendGoalUsed).HasColumnName("isRecomendGoalUsed");
            entity.Property(e => e.BaseActLevel).HasColumnName("baseActLevel");
            entity.Property(e => e.MetricParam).HasColumnName("metricParam");
            entity.Property(e => e.Timezone).HasColumnName("timezone");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsUpgraded).HasColumnName("isUpgraded");
            entity.Property(e => e.LastWipeOn).HasColumnName("lastWipeOn");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.Property(e => e.ModifiedBy).HasColumnName("modifiedBy");
            entity.Property(e => e.GoalMode).HasColumnName("goalMode");
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_mprofile_muser");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.Gender).HasConstraintName("FK_mprofile_gender");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.BaseActLevel).HasConstraintName("FK_mprofile_baseActLevel");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.MetricParam).HasConstraintName("FK_mprofile_metricParam");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.GoalMode).HasConstraintName("FK_mprofile_goalMode");
        });

        modelBuilder.Entity<MConsumption>(entity =>
        {
            entity.ToTable("mconsumption");
            entity.HasKey(e => e.ConsumptionPk);
            entity.Property(e => e.ConsumptionPk).HasColumnName("consumptionPk");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Calories).HasColumnName("calories");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.HasIndex(e => new { e.Name, e.Calories });
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.CreatedBy).HasConstraintName("FK_mconsumption_createdBy");
        });

        modelBuilder.Entity<TConsumption>(entity =>
        {
            entity.ToTable("tconsumption");
            entity.HasKey(e => e.EntryPk);
            entity.Property(e => e.EntryPk).HasColumnName("entryPk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.ConsumptionId).HasColumnName("consumptionId");
            entity.Property(e => e.MealType).HasColumnName("mealType");
            entity.Property(e => e.EntryTimestamp).HasColumnName("entryTimestamp");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isDeletedOn");
            entity.HasIndex(e => new { e.UserId, e.EntryTimestamp });
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_tconsumption_muser");
            entity.HasOne<MConsumption>().WithMany().HasForeignKey(e => e.ConsumptionId).HasConstraintName("FK_tconsumption_mconsumption");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.MealType).HasConstraintName("FK_tconsumption_mealType");
        });

        modelBuilder.Entity<TDailyRecord>(entity =>
        {
            entity.ToTable("tdailyrecord");
            entity.HasKey(e => e.DailyRecordPk);
            entity.Property(e => e.DailyRecordPk).HasColumnName("dailyRecordPk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.RecordDate).HasColumnName("recordDate");
            entity.HasIndex(e => new { e.UserId, e.RecordDate }).IsUnique();
            entity.Property(e => e.CalorieCategory).HasColumnName("calorieCategory");
            entity.Property(e => e.PaToday).HasColumnName("paToday");
            entity.Property(e => e.EffectiveTdee).HasColumnName("effectiveTdee");
            entity.Property(e => e.EffectiveLimit).HasColumnName("effectiveLimit");
            entity.Property(e => e.ActualCalories).HasColumnName("actualCalories");
            entity.Property(e => e.CreatedVia).HasColumnName("createdVia");
            entity.Property(e => e.IsFrozen).HasColumnName("isFrozen");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isDeletedOn");
            entity.Property(e => e.WipeReason).HasColumnName("wipeReason");
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_tdailyrecord_muser");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.CalorieCategory).HasConstraintName("FK_tdailyrecord_calorieCategory");
            entity.HasOne<MParam>().WithMany().HasForeignKey(e => e.CreatedVia).HasConstraintName("FK_tdailyrecord_createdVia");
        });

        modelBuilder.Entity<TWeightLog>(entity =>
        {
            entity.ToTable("tweightlog");
            entity.HasKey(e => e.WeightLogPk);
            entity.Property(e => e.WeightLogPk).HasColumnName("weightLogPk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Weight).HasColumnName("weight").HasColumnType("numeric(5,2)");
            entity.Property(e => e.CheckpointDate).HasColumnName("checkpointDate").HasColumnType("date");
            entity.Property(e => e.CreatedOn).HasColumnName("createdOn");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.IsDeletedOn).HasColumnName("isDeletedOn");
            entity.Property(e => e.WipeReason).HasColumnName("wipeReason");
            entity.HasIndex(e => new { e.UserId, e.CheckpointDate }).IsUnique();
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_tweightlog_muser");
        });

        modelBuilder.Entity<TStreak>(entity =>
        {
            entity.ToTable("tstreak");
            entity.HasKey(e => e.StreakPk);
            entity.Property(e => e.StreakPk).HasColumnName("streakPk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CurrentStreak).HasColumnName("currentStreak");
            entity.Property(e => e.LastLoggedDate).HasColumnName("lastLoggedDate");
            entity.Property(e => e.IsStreakFrozen).HasColumnName("isStreakFrozen");
            entity.Property(e => e.WipeReason).HasColumnName("wipeReason");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_tstreak_muser");
        });

        modelBuilder.Entity<TFreeze>(entity =>
        {
            entity.ToTable("tfreeze");
            entity.HasKey(e => e.FreezePk);
            entity.Property(e => e.FreezePk).HasColumnName("freezePk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.StreakFreezeCount).HasColumnName("streakFreezeCount");
            entity.Property(e => e.WipeFreezeCount).HasColumnName("wipeFreezeCount");
            entity.Property(e => e.DaysSinceLastStreakFreeze).HasColumnName("daysSinceLastStreakFreeze");
            entity.Property(e => e.DaysSinceLastWipeFreeze).HasColumnName("daysSinceLastWipeFreeze");
            entity.Property(e => e.LastStreakFreezeGainedDate).HasColumnName("lastStreakFreezeGainedDate");
            entity.Property(e => e.LastWipeFreezeGainedDate).HasColumnName("lastWipeFreezeGainedDate");
            entity.Property(e => e.WipeReason).HasColumnName("wipeReason");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
            entity.HasOne<MUser>().WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_tfreeze_muser");
        });

        modelBuilder.Entity<TNotification>(entity =>
        {
            entity.ToTable("tnotification");
            entity.HasKey(e => e.NotificationPk);
            entity.Property(e => e.NotificationPk).HasColumnName("notificationPk");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.FcmToken).HasColumnName("fcmToken");
            entity.Property(e => e.ModifiedOn).HasColumnName("modifiedOn");
        });
    }
}
