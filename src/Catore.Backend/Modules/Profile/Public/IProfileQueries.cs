namespace Catore.Backend.Modules.Profile.Public;

public interface IProfileQueries
{
    Task<ProfileSummaryDto?> GetProfileSummary(Guid userId);
}
