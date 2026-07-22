namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountQueries
{
    Task<ProfileAccountSummaryDto?> GetProfileSummary(Guid userId);
    Task<ProfileFullDto?> GetFullProfile(Guid userId);
}
