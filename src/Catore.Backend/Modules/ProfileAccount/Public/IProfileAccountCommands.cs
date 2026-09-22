namespace Catore.Backend.Modules.ProfileAccount.Public;

public interface IProfileAccountCommands
{
    Task SetUpgraded(long userId);
    Task MarkWiped(long userId, DateTime wipedAt, string wipeReason);
    Task<UpdateProfileResultDto> UpdateProfile(long userId, UpdateProfileRequestDto request);
    Task<ActivityAssessmentResultDto> SubmitActivityAssessment(long userId, string workEnvironment, string exerciseFrequency);
    Task<TimezoneRefreshResultDto> RefreshTimezone(long userId, string newTimezone);
    Task<ChangeGoalModeResultDto> ChangeGoalMode(long userId, ChangeGoalModeRequestDto request);
}
