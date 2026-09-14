namespace Catore.Backend.Modules.Freeze.Internal;

internal class TFreeze
{
    public long FreezePk { get; set; }
    public long UserId { get; set; }
    public int StreakFreezeCount { get; set; }
    public int WipeFreezeCount { get; set; }
    public int DaysSinceLastStreakFreeze { get; set; }
    public int DaysSinceLastWipeFreeze { get; set; }
    public DateOnly? LastStreakFreezeGainedDate { get; set; }
    public DateOnly? LastWipeFreezeGainedDate { get; set; }
    public DateTime ModifiedOn { get; set; }
}
