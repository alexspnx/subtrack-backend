namespace SubTrack.Silo.HostedServices;

/// <summary>
/// Activates the grains that must be running before the system can serve traffic.
/// Grains are virtual actors and activate on first call, so only put grains here that genuinely need eager
/// activation (e.g. to start reminders or timers). For work that must finish before the silo accepts requests,
/// prefer an Orleans lifecycle participant (<c>ILifecycleParticipant&lt;ISiloLifecycle&gt;</c>) instead.
/// </summary>
internal sealed class GrainInitializationHostedService(ILogger<GrainInitializationHostedService> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Inject IGrainFactory and activate the grains that need it here, e.g.
        // await grainFactory.GetGrain<IMatchSchedulerGrain>(0).StartAsync();
        logger.LogInformation("Grain initialization completed.");
        return Task.CompletedTask;
    }
}
