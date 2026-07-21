namespace Catore.Backend.Modules.WeightTracking.Public;

public interface IWeightTrackingQueries
{
    Task<IReadOnlyList<WeightEntryDto>> GetWeightHistory(Guid userId, DateOnly startDate, DateOnly endDate);
}
