using Lutra.Application.BackgroundCommands;
using Microsoft.Extensions.Options;

namespace Lutra.API.BackgroundCommands;

/// <summary>
/// Hosted worker that polls the durable background command queue and executes due jobs.
/// Jobs persist across restarts and lease expiry, so browser disconnects cannot cancel them.
/// </summary>
public sealed class BackgroundCommandWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundCommandsOptions> options,
    ILogger<BackgroundCommandWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("Background command worker is disabled by configuration.");
            return;
        }

        var pollDelay = TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds));
        var batchSize = Math.Max(1, settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;

            try
            {
                for (var i = 0; i < batchSize; i++)
                {
                    using var scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<BackgroundCommandProcessor>();

                    if (!await processor.ProcessNextAsync(stoppingToken))
                    {
                        break;
                    }

                    processedAny = true;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background command worker iteration failed.");
            }

            if (!processedAny)
            {
                try
                {
                    await Task.Delay(pollDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}