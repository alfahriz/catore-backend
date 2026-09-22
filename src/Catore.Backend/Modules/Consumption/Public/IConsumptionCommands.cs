namespace Catore.Backend.Modules.Consumption.Public;

public interface IConsumptionCommands
{
    Task<AddEntriesResultDto> AddEntries(long userId, AddEntriesRequestDto request);
    Task WipeUserData(long userId, DateTime wipedAt, string wipeReason);
    Task MarkDayFrozen(long userId, DateOnly date);
    Task<DailyRecordDto?> UpdateDailyRecord(long userId, DateOnly date, UpdateDailyRecordRequestDto request);
}
