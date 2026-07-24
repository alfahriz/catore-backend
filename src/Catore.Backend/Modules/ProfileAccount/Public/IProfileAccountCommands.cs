namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountCommands
{
    Task SetUpgraded(Guid userId);
    Task MarkWiped(Guid userId, DateTime wipedAt);
    Task<UpdateProfileResultDto> UpdateProfile(Guid userId, UpdateProfileRequestDto request);
    Task<ActivityAssessmentResultDto> SubmitActivityAssessment(Guid userId, string workEnvironment, string exerciseFrequency);
    Task<TimezoneRefreshResultDto> RefreshTimezone(Guid userId, string newTimezone);
}
