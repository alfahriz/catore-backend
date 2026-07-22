namespace Catore.Backend.Modules.Streak.Internal;

internal class StreakState
{
    public Guid StreakStatePk { get; set; }
    public Guid UserId { get; set; }
    public int CurrentStreakCount { get; set; }
    public DateOnly? LastLoggedDate { get; set; }
    public bool StreakIsFrozen { get; set; }
    public DateTime ModifiedOn { get; set; }
}
