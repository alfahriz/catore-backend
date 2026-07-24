using Catore.Backend.Modules.ProfileAccount.Public;

namespace Catore.Backend.Modules.Streak.Internal;

internal class WipeCheckJob : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WipeCheckJob> _logger;

    public WipeCheckJob(IServiceProvider serviceProvider, ILogger<WipeCheckJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var profileQueries = scope.ServiceProvider.GetRequiredService<IProfileAccountQueries>();
                var streakService = scope.ServiceProvider.GetRequiredService<StreakService>();

                var userIds = await profileQueries.GetAllActiveUserIds();
                var utcNow = DateTime.UtcNow;

                foreach (var userId in userIds)
                {
                    try
                    {
                        await streakService.EvaluateWipeCheck(userId, utcNow);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating wipe-check for user {UserId}", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running wipe-check job");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }
}
