namespace Catore.Backend.Modules.Streak.Internal;

internal class TStreak
{
    public long StreakPk { get; set; }
    public long UserId { get; set; }
    public int CurrentStreak { get; set; }
    public DateOnly? LastLoggedDate { get; set; }
    public bool IsStreakFrozen { get; set; }
    public string? WipeReason { get; set; }
    public DateTime ModifiedOn { get; set; }
}
