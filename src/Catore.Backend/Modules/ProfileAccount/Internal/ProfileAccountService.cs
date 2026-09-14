using Microsoft.Extensions.DependencyInjection;
using Catore.Backend.Modules.Param.Public;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class ProfileAccountService : IProfileAccountQueries, IProfileAccountCommands
{
    private const string GenderParamType = "GENDER";
    private const string ActivityLevelParamType = "ACTIVITY_LEVEL";
    private const string MetricUnitParamType = "METRIC_UNIT";

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

    public async Task<ProfileAccountSummaryDto?> GetProfileSummary(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;
        return new ProfileAccountSummaryDto(profile.Timezone, profile.GoalWeight, profile.IsUpgraded);
    }

    public async Task SetUpgraded(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return;
        profile.IsUpgraded = true;
        await _repository.Update(profile);
    }

    public async Task MarkWiped(long userId, DateTime wipedAt)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return;
        profile.LastWipeOn = wipedAt;
        await _repository.Update(profile);
    }

    public async Task<ProfileFullDto?> GetFullProfile(long userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;

        var genderName = await _paramQueries.ResolveName(profile.Gender) ?? string.Empty;
        var activityLevelName = await _paramQueries.ResolveName(profile.BaseActLevel) ?? string.Empty;
        var metricParamName = await _paramQueries.ResolveName(profile.MetricParam) ?? string.Empty;

        var tdee = NutritionCalculator.CalculateTdee(profile.Weight, profile.Height, profile.Age, genderName, activityLevelName);
        var bmi = NutritionCalculator.CalculateBmi(profile.Weight, profile.Height);
        var bmiCategory = NutritionCalculator.GetBmiCategory(bmi);
        var categoryLimits = NutritionCalculator.CalculateAllCategoryLimits(tdee, paToday: false);

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
            Math.Round(tdee, 0),
            Math.Round(bmi, 1),
            bmiCategory,
            categoryLimits.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 0))
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

        // Validasi goal weight (kalau diisi)
        if (request.GoalWeight.HasValue)
        {
            var currentWeight = request.WeightCurrent ?? profile.Weight;
            var height = request.Height ?? profile.Height;

            if (request.GoalWeight.Value >= currentWeight)
            {
                return new UpdateProfileResultDto(false, "Goal weight must be lower than current weight");
            }

            var floor = NutritionCalculator.CalculateGoalWeightFloor(height);
            if (request.GoalWeight.Value < floor)
            {
                return new UpdateProfileResultDto(false, $"Goal weight cannot be below {floor:F1} kg (health safety floor)");
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

    public async Task<EffectiveLimitDto?> CalculateLimit(long userId, string deficitCategory, bool paToday)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;

        var genderName = await _paramQueries.ResolveName(profile.Gender) ?? string.Empty;
        var activityLevelName = await _paramQueries.ResolveName(profile.BaseActLevel) ?? string.Empty;

        var tdee = NutritionCalculator.CalculateTdee(profile.Weight, profile.Height, profile.Age, genderName, activityLevelName);
        var limit = NutritionCalculator.CalculateDailyLimit(tdee, deficitCategory, paToday);

        return new EffectiveLimitDto(Math.Round(tdee, 0), Math.Round(limit, 0));
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
}
