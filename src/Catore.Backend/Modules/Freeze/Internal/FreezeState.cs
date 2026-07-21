namespace Catore.Backend.Modules.Freeze.Internal;

internal class FreezeState
{
    public Guid FreezeStatePk { get; set; }
    public Guid UserId { get; set; }
    public int StreakFreezeCount { get; set; }
    public int WipeFreezeCount { get; set; }
    public int DaysSinceLastStreakFreeze { get; set; }
    public int DaysSinceLastWipeFreeze { get; set; }
    public DateTime LastUpdated { get; set; }
}
