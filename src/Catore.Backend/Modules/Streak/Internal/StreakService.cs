using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.Streak.Internal;

// STUB: modul Streak belum diimplementasi beneran.
// TODO: implementasi asli logic grace window (Section 5.1) & freeze (Section 5.6).
internal class StreakService : IStreakQueries, IStreakCommands
{
    private readonly ILogger<StreakService> _logger;

    public StreakService(ILogger<StreakService> logger)
    {
        _logger = logger;
    }

    public Task<bool> HasActiveGraceWindow(Guid userId)
    {
        _logger.LogInformation("[STUB] HasActiveGraceWindow for {UserId} -> always false", userId);
        return Task.FromResult(false);
    }

    public Task FreezeStreak(Guid userId)
    {
        _logger.LogInformation("[STUB] FreezeStreak for {UserId}", userId);
        return Task.CompletedTask;
    }
}
