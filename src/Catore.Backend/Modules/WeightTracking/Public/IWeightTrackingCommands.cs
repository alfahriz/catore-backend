namespace Catore.Backend.Modules.WeightTracking.Public;

public interface IWeightTrackingCommands
{
    Task WipeUserData(long userId, DateTime wipedAt);
    Task<AddWeightLogResultDto> AddOrUpdateWeightLog(long userId, DateTime entryTimestamp, decimal weightValue);
}
