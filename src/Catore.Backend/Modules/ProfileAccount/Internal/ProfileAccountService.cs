using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.Freeze.Public;
using Catore.Backend.Modules.Param.Public;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Public;
using Catore.Backend.Modules.WeightTracking.Public;

namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class ProfileAccountService : IProfileAccountQueries, IProfileAccountCommands
{
    private const string GenderParamType = "GENDER";
    private const string ActivityLevelParamType = "ACTIVITY_LEVEL";
    private const string MetricUnitParamType = "METRIC_UNIT";
    private const string GoalModeParamType = "GOAL_MODE";
    private const string DefaultGoalMode = "Cutting";

    private readonly ProfileAccountRepository _repository;
    private readonly IParamQueries _paramQueries;
    private readonly IServiceProvider _serviceProvider;

    public ProfileAccountService(ProfileAccountRepository repository, IParamQueries paramQueries, IServiceProvider serviceProvider)
    {
        _repository = repository;
        _paramQueries = paramQueries;
        _serviceProvider = serviceProvider;
    }

    // Resolve on-demand (bukan constructor injection) — StreakService juga depend balik ke
    // IProfileAccountQueries (butuh timezone buat derive grace window), circular kalau di-inject langsung.
    private IStreakQueries StreakQueries => _serviceProvider.GetRequiredService<IStreakQueries>();
    // Dipakai ChangeGoalMode buat orkestrasi wipe-ganti-mode (pola sama StreakService.EvaluateWipeCheck).
    private IStreakCommands StreakCommands => _serviceProvider.GetRequiredService<IStreakCommands>();
    private IConsumptionCommands ConsumptionCommands => _serviceProvider.GetRequiredService<IConsumptionCommands>();
    private IWeightTrackingCommands WeightTrackingCommands => _serviceProvider.GetRequiredService<IWeightTrackingCommands>();
    private IFreezeCommands FreezeCommands => _serviceProvider.GetRequiredService<IFreezeCommands>();

    public async Task<ProfileAccountSummaryDto?> GetProfileSummary(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;
        var goalModeName = await _paramQueries.ResolveName(profile.GoalMode) ?? DefaultGoalMode;
        return new ProfileAccountSummaryDto(profile.Timezone, profile.GoalWeight, profile.IsUpgraded, goalModeName);
    }

    public async Task SetUpgraded(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return;
        profile.IsUpgraded = true;
        await _repository.Update(profile);
    }

    public async Task MarkWiped(long userId, DateTime wipedAt, string wipeReason)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return;
        profile.LastWipeOn = wipedAt;
        // mprofile sendiri gak py kolom wipeReason (cuma tweightlog/tstreak/tfreeze/
        // tdailyrecord) -- LastWipeOn timestamp + WipeReason di tabel2 itu udah cukup
        // buat telusuri alasan wipe pakai timestamp yg sama.
        await _repository.Update(profile);
    }

    public async Task<ProfileFullDto?> GetFullProfile(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;

        var genderName = await _paramQueries.ResolveName(profile.Gender) ?? string.Empty;
        var activityLevelName = await _paramQueries.ResolveName(profile.BaseActLevel) ?? string.Empty;
        var metricParamName = await _paramQueries.ResolveName(profile.MetricParam) ?? string.Empty;
        var goalModeName = await _paramQueries.ResolveName(profile.GoalMode) ?? DefaultGoalMode;

        var tdee = NutritionCalculator.CalculateTdee(profile.Weight, profile.Height, profile.Age, genderName, activityLevelName);
        var bmi = NutritionCalculator.CalculateBmi(profile.Weight, profile.Height);
        var bmiCategory = NutritionCalculator.GetBmiCategory(bmi);
        var categoryLimits = NutritionCalculator.CalculateAllCategoryLimits(tdee, goalModeName, paToday: false);

        return new ProfileFullDto(
            profile.Height,
            profile.Weight,
            profile.Age,
            genderName,
            profile.Name,
            activityLevelName,
            profile.GoalWeight,
            !profile.IsRecomendGoalUsed,
            metricParamName,
            profile.Timezone,
            profile.IsUpgraded,
            goalModeName,
            Math.Round(tdee, 0),
            Math.Round(bmi, 1),
            bmiCategory,
            categoryLimits.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 0)),
            profile.LastWipeOn
        );
    }

    public async Task<UpdateProfileResultDto> UpdateProfile(long userId, UpdateProfileRequestDto request)
    {
        var profile = await _repository.GetByUserId(userId);
        var isNew = profile is null;

        if (profile is null)
        {
            profile = new MProfile
            {
                UserId = userId
            };
        }

        // Validasi goal weight (kalau diisi) — arah beda tergantung goalMode. Endpoint ini
        // (PUT /profile) HANYA valid dipakai profile mode Cutting (default/existing behavior);
        // Bulking/Maintain ganti goal weight lewat alur ChangeGoalMode (POST /profile/goal-mode)
        // krn perubahan goal weight di 2 mode itu selalu berbarengan sama transisi mode/wipe.
        if (request.GoalWeight.HasValue)
        {
            var goalModeName = await _paramQueries.ResolveName(profile.GoalMode) ?? DefaultGoalMode;
            if (goalModeName != "Cutting")
            {
                return new UpdateProfileResultDto(false, "Use goal-mode endpoint to change goal weight in Bulking/Maintain mode");
            }

            var currentWeight = request.WeightCurrent ?? profile.Weight;
            var height = request.Height ?? profile.Height;

            if (request.GoalWeight.Value >= currentWeight)
            {
                return new UpdateProfileResultDto(false, "Goal weight must be lower than current weight");
            }

            var floor = NutritionCalculator.CalculateGoalWeightFloor(height);
            if (request.GoalWeight.Value < floor)
            {
                return new UpdateProfileResultDto(false, $"Goal weight cannot be below {floor.ToString("F1", CultureInfo.InvariantCulture)} kg (health safety floor)");
            }
        }

        if (request.Height.HasValue) profile.Height = request.Height.Value;
        if (request.WeightCurrent.HasValue) profile.Weight = request.WeightCurrent.Value;
        if (request.Age.HasValue) profile.Age = request.Age.Value;
        if (request.Gender is not null) profile.Gender = await _paramQueries.ResolvePk(GenderParamType, request.Gender);
        if (request.DisplayName is not null) profile.Name = request.DisplayName;
        if (request.GoalWeight.HasValue)
        {
            profile.GoalWeight = request.GoalWeight.Value;
            profile.IsRecomendGoalUsed = false;
        }
        if (request.MetricPreference is not null) profile.MetricParam = await _paramQueries.ResolvePk(MetricUnitParamType, request.MetricPreference);
        if (request.Timezone is not null) profile.Timezone = request.Timezone;

        profile.ModifiedOn = DateTime.UtcNow;
        profile.ModifiedBy = userId.ToString();

        if (isNew)
        {
            profile.CreatedOn = DateTime.UtcNow;
            profile.CreatedBy = userId;
            await _repository.Add(profile);
        }
        else
        {
            await _repository.Update(profile);
        }

        return new UpdateProfileResultDto(true, null);
    }

    public async Task<ActivityAssessmentResultDto> SubmitActivityAssessment(long userId, string workEnvironment, string exerciseFrequency)
    {
        var level = MapActivityAssessment(workEnvironment, exerciseFrequency);

        var profile = await _repository.GetByUserId(userId);
        if (profile is null)
        {
            return new ActivityAssessmentResultDto(false, level);
        }

        profile.BaseActLevel = await _paramQueries.ResolvePk(ActivityLevelParamType, level);
        await _repository.Update(profile);

        return new ActivityAssessmentResultDto(true, level);
    }

    private static string MapActivityAssessment(string workEnvironment, string exerciseFrequency)
    {
        // Mapping sederhana: kombinasi jenis pekerjaan (indoor/outdoor) + frekuensi olahraga/minggu
        var isOutdoorOrActiveWork = workEnvironment.Equals("outdoor", StringComparison.OrdinalIgnoreCase);
        var exerciseDays = int.TryParse(exerciseFrequency, out var days) ? days : 0;

        if (exerciseDays >= 6) return "Very active";
        if (exerciseDays >= 3) return isOutdoorOrActiveWork ? "Very active" : "Moderately active";
        if (exerciseDays >= 1) return isOutdoorOrActiveWork ? "Moderately active" : "Lightly active";
        return isOutdoorOrActiveWork ? "Lightly active" : "Sedentary";
    }

    public async Task<EffectiveLimitDto?> CalculateLimit(long userId, string calorieCategory, bool paToday)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;

        var genderName = await _paramQueries.ResolveName(profile.Gender) ?? string.Empty;
        var activityLevelName = await _paramQueries.ResolveName(profile.BaseActLevel) ?? string.Empty;
        var goalModeName = await _paramQueries.ResolveName(profile.GoalMode) ?? DefaultGoalMode;

        var tdee = NutritionCalculator.CalculateTdee(profile.Weight, profile.Height, profile.Age, genderName, activityLevelName);
        var limit = NutritionCalculator.CalculateDailyLimit(tdee, goalModeName, calorieCategory, paToday);

        return new EffectiveLimitDto(Math.Round(tdee, 0), Math.Round(limit, 0));
    }

    // Dipanggil WeightTrackingService.AddOrUpdateWeightLog tiap kali user log berat baru,
    // cuma relevan kalau goalMode==Maintain (caller filter dulu). goalWeight di Maintain =
    // baseline weight saat ChangeGoalMode dieksekusi (lihat NutritionCalculator.CheckMaintainRangeExceeded).
    // currentWeight WAJIB dikirim caller (bukan baca profile.Weight) -- BUG DITEMUKAN &
    // DIFIX 2026-09-22: mprofile.weight TIDAK PERNAH diupdate oleh AddOrUpdateWeightLog
    // (yang diupdate cuma tweightlog, histori mingguan terpisah) -- kalau fungsi ini baca
    // profile.Weight, dia selalu dapat angka LAMA/stale, pagar Maintain gak akan pernah
    // ke-trigger meski user udah log berat jauh berbeda. Ketemu pas testing manual (T9).
    public async Task<MaintainRangeCheckDto?> CheckMaintainRange(long userId, decimal currentWeight)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null || !profile.GoalWeight.HasValue) return null;

        var genderName = await _paramQueries.ResolveName(profile.Gender) ?? string.Empty;
        var activityLevelName = await _paramQueries.ResolveName(profile.BaseActLevel) ?? string.Empty;

        var actualTdee = NutritionCalculator.CalculateTdee(currentWeight, profile.Height, profile.Age, genderName, activityLevelName);
        var baselineTdee = NutritionCalculator.CalculateTdee(profile.GoalWeight.Value, profile.Height, profile.Age, genderName, activityLevelName);

        var suggestedMode = NutritionCalculator.CheckMaintainRangeExceeded(actualTdee, baselineTdee);
        return new MaintainRangeCheckDto(suggestedMode is not null, suggestedMode);
    }

    public async Task<IReadOnlyList<long>> GetAllActiveUserIds()
    {
        return await _repository.GetAllActiveUserIds();
    }

    public async Task<TimezoneRefreshResultDto> RefreshTimezone(long userId, string newTimezone)
    {
        var hasActiveGraceWindow = await StreakQueries.HasActiveGraceWindow(userId);
        if (hasActiveGraceWindow)
        {
            return new TimezoneRefreshResultDto(false, "Timezone cannot be changed while a grace window is active");
        }

        var profile = await _repository.GetByUserId(userId);
        if (profile is null)
        {
            return new TimezoneRefreshResultDto(false, "Profile not found");
        }

        profile.Timezone = newTimezone;
        await _repository.Update(profile);

        return new TimezoneRefreshResultDto(true, null);
    }

    // Ganti mode (Cutting/Bulking/Maintain) manual dari Profile — TRIGGER WIPE PENUH,
    // WipeReason="Manual" (beda dari wipe rutin "LostStreak" di StreakService.EvaluateWipeCheck).
    // Histori tweightlog TETAP ADA (soft-delete, sama pola wipe rutin) -- prinsip lama "Reality
    // garis gak pernah hilang total" tetap dipegang, cuma ditandai milik periode sebelumnya.
    // Requirement lengkap: Workspace/Catore - Shared/memory/feature-mode-cutting-bulking-maintain.md
    public async Task<ChangeGoalModeResultDto> ChangeGoalMode(long userId, ChangeGoalModeRequestDto request)
    {
        var validModes = new[] { "Cutting", "Bulking", "Maintain" };
        if (!validModes.Contains(request.NewGoalMode))
        {
            return new ChangeGoalModeResultDto(false, "Invalid goal mode");
        }

        var profile = await _repository.GetByUserId(userId);
        if (profile is null)
        {
            return new ChangeGoalModeResultDto(false, "Profile not found");
        }

        var currentGoalModeName = await _paramQueries.ResolveName(profile.GoalMode) ?? DefaultGoalMode;
        if (currentGoalModeName == request.NewGoalMode)
        {
            return new ChangeGoalModeResultDto(false, "Already in this goal mode");
        }

        // Maintain gak butuh goalWeight eksplisit (auto = current weight, lihat requirement) —
        // Cutting/Bulking WAJIB dikasih goalWeight baru krn floor/ceiling beda arah per mode.
        if (request.NewGoalMode != "Maintain" && !request.GoalWeight.HasValue)
        {
            return new ChangeGoalModeResultDto(false, "Goal weight is required for Cutting/Bulking mode");
        }

        if (request.GoalWeight.HasValue)
        {
            // Arah goal weight harus konsisten sama makna mode — Cutting = turun (goal < current),
            // Bulking = naik (goal > current). Floor Cutting (PRD 4.1, HARD) tetap dicek di sini;
            // ceiling Bulking (BMI27) SENGAJA TIDAK dicek (soft/advisory sesuai requirement —
            // user boleh lewat, cuma konsekuensi natural di estimasi Progress Projection).
            if (request.NewGoalMode == "Cutting")
            {
                if (request.GoalWeight.Value >= profile.Weight)
                {
                    return new ChangeGoalModeResultDto(false, "Goal weight must be lower than current weight for Cutting mode");
                }
                var floor = NutritionCalculator.CalculateGoalWeightFloor(profile.Height);
                if (request.GoalWeight.Value < floor)
                {
                    return new ChangeGoalModeResultDto(false, $"Goal weight cannot be below {floor.ToString("F1", CultureInfo.InvariantCulture)} kg (health safety floor)");
                }
            }
            else if (request.NewGoalMode == "Bulking")
            {
                if (request.GoalWeight.Value <= profile.Weight)
                {
                    return new ChangeGoalModeResultDto(false, "Goal weight must be higher than current weight for Bulking mode");
                }
            }
        }

        var utcNow = DateTime.UtcNow;
        var newGoalModePk = await _paramQueries.ResolvePk(GoalModeParamType, request.NewGoalMode);

        // Auto-transisi dari saran pagar Maintain: TANPA WIPE, streak lanjut jalan (requirement
        // eksplisit) — beda dari ganti mode manual dari Profile yang SELALU wipe total.
        if (!request.FromMaintainSuggestion)
        {
            const string wipeReason = "Manual";
            // Orkestrasi wipe lintas-module — pola sama StreakService.EvaluateWipeCheck (wipe
            // rutin), tanpa DB transaction eksplisit di sini krn tiap WipeUserData/ResetStreak/
            // ResetAfterWipe sudah SaveChanges sendiri2 (konsisten pola existing, bukan regresi baru).
            await ConsumptionCommands.WipeUserData(userId, utcNow, wipeReason);
            await WeightTrackingCommands.WipeUserData(userId, utcNow, wipeReason);
            await StreakCommands.ResetStreak(userId, wipeReason);
            await FreezeCommands.ResetAfterWipe(userId, wipeReason);
            profile.IsUpgraded = false;
            profile.LastWipeOn = utcNow;
        }

        profile.GoalMode = newGoalModePk;
        profile.GoalWeight = request.NewGoalMode == "Maintain" ? profile.Weight : request.GoalWeight;
        profile.IsRecomendGoalUsed = false;
        await _repository.Update(profile);

        return new ChangeGoalModeResultDto(true, null);
    }
}
