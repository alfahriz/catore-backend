namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountQueries
{
    Task<ProfileAccountSummaryDto?> GetProfileSummary(long userId);
    Task<ProfileFullDto?> GetFullProfile(long userId);
    Task<EffectiveLimitDto?> CalculateLimit(long userId, string deficitCategory, bool paToday);
    Task<IReadOnlyList<long>> GetAllActiveUserIds();
}
