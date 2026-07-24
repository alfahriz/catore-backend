using Catore.Backend.Modules.WeightTracking.Public;

namespace Catore.Backend.Modules.WeightTracking.Internal;

internal class WeightTrackingService : IWeightTrackingQueries, IWeightTrackingCommands
{
    private readonly WeightTrackingRepository _repository;

    public WeightTrackingService(WeightTrackingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WeightEntryDto>> GetWeightHistory(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var entries = await _repository.GetByUserIdAndRange(userId, startDate, endDate);
        return entries
            .Select(e => new WeightEntryDto(DateOnly.FromDateTime(e.LoggedAt), e.WeightValue))
            .ToList();
    }

    public async Task WipeUserData(Guid userId, DateTime wipedAt)
    {
        await _repository.WipeUserData(userId, wipedAt);
    }
}
