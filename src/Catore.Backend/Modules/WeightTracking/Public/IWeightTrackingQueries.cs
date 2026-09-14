namespace Catore.Backend.Modules.WeightTracking.Public;

public interface IWeightTrackingQueries
{
    Task<IReadOnlyList<WeightEntryDto>> GetWeightHistory(long userId, DateOnly startDate, DateOnly endDate);
}
