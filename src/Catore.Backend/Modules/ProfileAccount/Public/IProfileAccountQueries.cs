namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountQueries
{
    Task<ProfileAccountSummaryDto?> GetProfileSummary(long userId);
    Task<ProfileFullDto?> GetFullProfile(long userId);
    Task<EffectiveLimitDto?> CalculateLimit(long userId, string calorieCategory, bool paToday);
    Task<MaintainRangeCheckDto?> CheckMaintainRange(long userId, decimal currentWeight);
    Task<IReadOnlyList<long>> GetAllActiveUserIds();
}
