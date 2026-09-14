namespace Catore.Backend.Modules.ProfileAccount.Internal;

internal class MProfile
{
    public long ProfilePk { get; set; }
    public long UserId { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public int Age { get; set; }
    public long? Gender { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? BaseActLevel { get; set; }
    public decimal? GoalWeight { get; set; }
    public bool IsRecomendGoalUsed { get; set; }
    public long? MetricParam { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public DateTime ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsUpgraded { get; set; }
    public DateTime? LastWipeOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public long? CreatedBy { get; set; }
}
