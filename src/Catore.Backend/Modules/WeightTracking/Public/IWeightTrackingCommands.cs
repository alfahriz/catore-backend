namespace Catore.Backend.Modules.WeightTracking.Public;

public interface IWeightTrackingCommands
{
    Task WipeUserData(long userId, DateTime wipedAt, string wipeReason);
    Task<AddWeightLogResultDto> AddOrUpdateWeightLog(long userId, DateTime entryTimestamp, decimal weightValue);
}
