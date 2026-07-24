namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountQueries
{
    Task<ProfileAccountSummaryDto?> GetProfileSummary(Guid userId);
    Task<ProfileFullDto?> GetFullProfile(Guid userId);
    Task<EffectiveLimitDto?> CalculateLimit(Guid userId, string deficitCategory, bool paToday);
    Task<IReadOnlyList<Guid>> GetAllActiveUserIds();
}
