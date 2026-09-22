namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal static class NutritionCalculator
{
    // Key dictionary ini Title Case, SAMA PERSIS dengan mparam.name (seed.sql) — supaya
    // caller (ProfileAccountService dkk) bisa lempar nama hasil resolve mparam LANGSUNG
    // tanpa perlu translasi format tambahan.
    private static readonly Dictionary<string, decimal> ActivityMultipliers = new()
    {
        ["Sedentary"] = 1.2m,
        ["Lightly active"] = 1.375m,
        ["Moderately active"] = 1.55m,
        ["Very active"] = 1.725m
    };

    private static readonly Dictionary<string, int> CategoryOffsets = new()
    {
        ["Recovery"] = 0,
        ["Soft"] = -300,
        ["Mid"] = -400,
        ["Hard"] = -500
    };

    // Bulking (baru, fitur 3-mode Cutting/Bulking/Maintain) — TANPA "Recovery": gak ada
    // use-case netral pas lagi bulking (beda dari Cutting yg butuh jeda dari tekanan
    // defisit pas capek/sakit). Skala offset sengaja lebih kecil dari Cutting (max +350
    // vs max -500) -- surplus aman secara fisiologis harus lebih konservatif dari defisit.
    private static readonly Dictionary<string, int> BulkingCategoryOffsets = new()
    {
        ["Mild"] = 150,
        ["Moderate"] = 250,
        ["Aggressive"] = 350
    };

    // Maintain gak py kategori pilihan (1 titik TDEE) -- rentang pagar diambil dari
    // kategori TEREKSTREM tiap mode (Cutting Hard -500, Bulking Aggressive +350), BUKAN
    // dari Recovery (yg 0 di kedua sisi bikin rentang nol lebar, gak masuk akal jadi pagar).
    public const int MaintainRangeLowOffset = -500;
    public const int MaintainRangeHighOffset = 350;

    public static decimal CalculateBmr(decimal weightKg, decimal heightCm, int age, string gender)
    {
        var baseBmr = 10 * weightKg + 6.25m * heightCm - 5 * age;
        return gender.Equals("Male", StringComparison.OrdinalIgnoreCase) ? baseBmr + 5 : baseBmr - 161;
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

    // goalMode: "Cutting" (default kalau null/unrecognized, backward-compat profile lama)
    // / "Bulking" / "Maintain". Maintain mengabaikan calorieCategory (gak py kategori),
    // selalu return TDEE + PA bonus doang.
    public static decimal CalculateDailyLimit(decimal tdee, string? goalMode, string calorieCategory, bool paToday)
    {
        if (goalMode == "Maintain")
        {
            return tdee + (paToday ? 200 : 0);
        }

        var offsets = goalMode == "Bulking" ? BulkingCategoryOffsets : CategoryOffsets;
        var offset = offsets.GetValueOrDefault(calorieCategory, 0);
        return tdee + offset + (paToday ? 200 : 0);
    }

    public static IReadOnlyDictionary<string, decimal> CalculateAllCategoryLimits(decimal tdee, string? goalMode, bool paToday)
    {
        if (goalMode == "Maintain")
        {
            return new Dictionary<string, decimal> { ["Maintain"] = tdee + (paToday ? 200 : 0) };
        }

        var offsets = goalMode == "Bulking" ? BulkingCategoryOffsets : CategoryOffsets;
        return offsets.ToDictionary(
            kv => kv.Key,
            kv => CalculateDailyLimit(tdee, goalMode, kv.Key, paToday));
    }

    // Pagar Maintain: goalWeight di mode Maintain = weight SAAT MULAI Maintain (baseline,
    // di-set ChangeGoalMode). TDEE baseline dihitung dari situ, dibanding TDEE AKTUAL
    // (dari weight sekarang, height/age/gender/activityLevel dianggap gak berubah dalam
    // rentang waktu pendek). Kalau TDEE aktual tembus [TDEE_baseline-500, +350] --
    // current weight udah bergeser cukup jauh dari titik mulai Maintain -- saran pindah mode.
    // null = masih dalam rentang aman, "Cutting"/"Bulking" = arah saran pindah.
    public static string? CheckMaintainRangeExceeded(decimal actualTdee, decimal baselineTdee)
    {
        var low = baselineTdee + MaintainRangeLowOffset;
        var high = baselineTdee + MaintainRangeHighOffset;
        if (actualTdee > high) return "Bulking"; // TDEE naik jauh = berat naik jauh -> saran Bulking
        if (actualTdee < low) return "Cutting";  // TDEE turun jauh = berat turun jauh -> saran Cutting
        return null;
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

    // Bulking ceiling = berat pada BMI 27 (batas overweight WHO). SOFT ceiling by
    // requirement (user BOLEH lewat, cuma nunjukin estimasi waktu lebih lama di Progress
    // Projection) -- fungsi ini CUMA buat referensi/display, BUKAN dipakai reject input.
    public static decimal CalculateBulkingCeiling(decimal heightCm)
    {
        var heightM = heightCm / 100;
        return 27m * heightM * heightM;
    }

}
