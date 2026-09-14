namespace Catore.Backend.Modules.Freeze.Public;

public interface IFreezeQueries
{
    Task<FreezeTokenSummaryDto?> GetAvailableTokens(long userId);
}
