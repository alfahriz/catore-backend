namespace Catore.Backend.Modules.Consumption.Public;

public interface IConsumptionQueries
{
    Task<IReadOnlyList<DailyTotalDto>> GetDailyTotalsForRange(Guid userId, DateOnly startDate, DateOnly endDate);
}
