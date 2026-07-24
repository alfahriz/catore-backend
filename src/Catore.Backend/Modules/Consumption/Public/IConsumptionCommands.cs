namespace Catore.Backend.Modules.Consumption.Public;

public interface IConsumptionCommands
{
    Task<AddEntriesResultDto> AddEntries(Guid userId, AddEntriesRequestDto request);
    Task WipeUserData(Guid userId, DateTime wipedAt);
    Task MarkDayFrozen(Guid userId, DateOnly date);
}
