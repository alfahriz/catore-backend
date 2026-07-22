using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class ProfileAccountService : IProfileAccountQueries, IProfileAccountCommands
{
    private readonly ProfileAccountRepository _repository;
    private readonly IStreakQueries _streakQueries;

    public ProfileAccountService(ProfileAccountRepository repository, IStreakQueries streakQueries)
    {
        _repository = repository;
        _streakQueries = streakQueries;
    }

    public async Task<ProfileAccountSummaryDto?> GetProfileSummary(Guid userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;
        return new ProfileAccountSummaryDto(profile.Timezone, profile.GoalWeight, profile.IsUpgraded);
    }

    public async Task SetUpgraded(Guid userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return;
        profile.IsUpgraded = true;
        await _repository.Update(profile);
    }

    public async Task<ProfileFullDto?> GetFullProfile(Guid userId)
    {
        var profile = await _repository.GetByUserId(userId);
        if (profile is null) return null;

        var tdee = NutritionCalculator.CalculateTdee(profile.WeightCurrent, profile.Height, profile.Age, profile.Gender, profile.BaselineActivityLevel);
        var bmi = NutritionCalculator.CalculateBmi(profile.WeightCurrent, profile.Height);
        var bmiCategory = NutritionCalculator.GetBmiCategory(bmi);
        var categoryLimits = NutritionCalculator.CalculateAllCategoryLimits(tdee, paToday: false);

        return new ProfileFullDto(
            profile.Height,
            profile.WeightCurrent,
            profile.Age,
            profile.Gender,
            profile.DisplayName,
            profile.BaselineActivityLevel,
            profile.GoalWeight,
            profile.GoalWeightIsManual,
            profile.MetricPreference,
            profile.Timezone,
            profile.IsUpgraded,
            Math.Round(tdee, 0),
            Math.Round(bmi, 1),
            bmiCategory,
            categoryLimits.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 0))
        );
    }

    public async Task<UpdateProfileResultDto> UpdateProfile(Guid userId, UpdateProfileRequestDto request)
    {
        var profile = await _repository.GetByUserId(userId);
        var isNew = profile is null;

        if (profile is null)
        {
            profile = new ProfileAccount
            {
                ProfileAccountPk = Guid.NewGuid(),
                UserId = userId
            };
        }

        // Validasi goal weight (kalau diisi)
        if (request.GoalWeight.HasValue)
        {
            var currentWeight = request.WeightCurrent ?? profile.WeightCurrent;
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
        if (request.WeightCurrent.HasValue) profile.WeightCurrent = request.WeightCurrent.Value;
        if (request.Age.HasValue) profile.Age = request.Age.Value;
        if (request.Gender is not null) profile.Gender = request.Gender;
        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName;
        if (request.GoalWeight.HasValue)
        {
            profile.GoalWeight = request.GoalWeight.Value;
            profile.GoalWeightIsManual = true;
        }
        if (request.MetricPreference is not null) profile.MetricPreference = request.MetricPreference;
        if (request.Timezone is not null) profile.Timezone = request.Timezone;

        profile.ModifiedOn = DateTime.UtcNow;

        if (isNew)
        {
            await _repository.Add(profile);
        }
        else
        {
            await _repository.Update(profile);
        }

        return new UpdateProfileResultDto(true, null);
    }

    public async Task<ActivityAssessmentResultDto> SubmitActivityAssessment(Guid userId, string workEnvironment, string exerciseFrequency)
    {
        var level = MapActivityAssessment(workEnvironment, exerciseFrequency);

        var profile = await _repository.GetByUserId(userId);
        if (profile is null)
        {
            return new ActivityAssessmentResultDto(false, level);
        }

        profile.BaselineActivityLevel = level;
        await _repository.Update(profile);

        return new ActivityAssessmentResultDto(true, level);
    }

    private static string MapActivityAssessment(string workEnvironment, string exerciseFrequency)
    {
        // Mapping sederhana: kombinasi jenis pekerjaan (indoor/outdoor) + frekuensi olahraga/minggu
        var isOutdoorOrActiveWork = workEnvironment.Equals("outdoor", StringComparison.OrdinalIgnoreCase);
        var exerciseDays = int.TryParse(exerciseFrequency, out var days) ? days : 0;

        if (exerciseDays >= 6) return "very_active";
        if (exerciseDays >= 3) return isOutdoorOrActiveWork ? "very_active" : "moderately_active";
        if (exerciseDays >= 1) return isOutdoorOrActiveWork ? "moderately_active" : "lightly_active";
        return isOutdoorOrActiveWork ? "lightly_active" : "sedentary";
    }

    public async Task<TimezoneRefreshResultDto> RefreshTimezone(Guid userId, string newTimezone)
    {
        var hasActiveGraceWindow = await _streakQueries.HasActiveGraceWindow(userId);
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
