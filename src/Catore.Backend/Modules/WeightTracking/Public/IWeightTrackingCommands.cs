namespace Catore.Backend.Modules.WeightTracking.Public;

public interface IWeightTrackingCommands
{
    Task WipeUserData(Guid userId, DateTime wipedAt);
    Task<AddWeightLogResultDto> AddOrUpdateWeightLog(Guid userId, DateTime entryTimestamp, decimal weightValue);
}
