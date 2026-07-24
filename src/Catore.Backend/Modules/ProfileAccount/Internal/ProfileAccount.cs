namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class ProfileAccount
{
    public Guid ProfileAccountPk { get; set; }
    public Guid UserId { get; set; }
    public decimal Height { get; set; }
    public decimal WeightCurrent { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BaselineActivityLevel { get; set; } = string.Empty;
    public decimal? GoalWeight { get; set; }
    public bool GoalWeightIsManual { get; set; }
    public string MetricPreference { get; set; } = "metric";
    public string Timezone { get; set; } = string.Empty;
    public DateTime ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsUpgraded { get; set; }
    public DateTime? LastWipedAt { get; set; }
}
