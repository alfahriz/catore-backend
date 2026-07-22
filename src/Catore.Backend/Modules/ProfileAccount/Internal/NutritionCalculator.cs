namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal static class NutritionCalculator
{
    private static readonly Dictionary<string, decimal> ActivityMultipliers = new()
    {
        ["sedentary"] = 1.2m,
        ["lightly_active"] = 1.375m,
        ["moderately_active"] = 1.55m,
        ["very_active"] = 1.725m
    };

    private static readonly Dictionary<string, int> CategoryOffsets = new()
    {
        ["recovery"] = 0,
        ["soft"] = -300,
        ["mid"] = -400,
        ["hard"] = -500
    };

    public static decimal CalculateBmr(decimal weightKg, decimal heightCm, int age, string gender)
    {
        var baseBmr = 10 * weightKg + 6.25m * heightCm - 5 * age;
        return gender.Equals("male", StringComparison.OrdinalIgnoreCase) ? baseBmr + 5 : baseBmr - 161;
    }

    public static decimal CalculateTdee(decimal weightKg, decimal heightCm, int age, string gender, string baselineActivityLevel)
    {
        var bmr = CalculateBmr(weightKg, heightCm, age, gender);
        var multiplier = ActivityMultipliers.GetValueOrDefault(baselineActivityLevel, 1.2m);
        return bmr * multiplier;
    }

    public static decimal CalculateBmi(decimal weightKg, decimal heightCm)
    {
        var heightM = heightCm / 100;
        return weightKg / (heightM * heightM);
    }

    public static string GetBmiCategory(decimal bmi)
    {
        if (bmi < 18.5m) return "Underweight";
        if (bmi < 25m) return "Normal";
        if (bmi < 30m) return "Overweight";
        return "Obese";
    }

    public static decimal CalculateDailyLimit(decimal tdee, string deficitCategory, bool paToday)
    {
        var offset = CategoryOffsets.GetValueOrDefault(deficitCategory, 0);
        return tdee + offset + (paToday ? 200 : 0);
    }

    public static IReadOnlyDictionary<string, decimal> CalculateAllCategoryLimits(decimal tdee, bool paToday)
    {
        return CategoryOffsets.ToDictionary(
            kv => kv.Key,
            kv => CalculateDailyLimit(tdee, kv.Key, paToday));
    }

    public static decimal CalculateIdealWeight(decimal heightCm, decimal targetBmi = 22m)
    {
        var heightM = heightCm / 100;
        return targetBmi * heightM * heightM;
    }

    public static decimal CalculateGoalWeightFloor(decimal heightCm)
    {
        var practicalFloor = CalculateIdealWeight(heightCm) - 10;
        var bmi16Floor = 16m * (heightCm / 100) * (heightCm / 100);
        return Math.Max(practicalFloor, bmi16Floor);
    }
}
