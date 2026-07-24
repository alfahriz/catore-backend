namespace Catore.Backend.Modules.Auth.Internal;

internal class UnverifiedAccountCleanupJob : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnverifiedAccountCleanupJob> _logger;

    public UnverifiedAccountCleanupJob(IServiceProvider serviceProvider, ILogger<UnverifiedAccountCleanupJob> logger)
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
                var repository = scope.ServiceProvider.GetRequiredService<AuthRepository>();

                var expired = await repository.GetExpiredUnverified(DateTime.UtcNow);
                foreach (var account in expired)
                {
                    await repository.Delete(account);
                    _logger.LogInformation("Deleted unverified expired account {Email}", account.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running unverified account cleanup job");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }
}
