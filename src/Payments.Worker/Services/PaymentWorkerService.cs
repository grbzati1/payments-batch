using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Infrastructure.Processing;

namespace Payments.Worker.Services;

/// <summary>
/// Background service that continuously processes pending payments at configured intervals.
/// Creates a new payment processor scope for each iteration to ensure proper dependency lifecycle management.
/// </summary>
/// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies for each processing cycle.</param>
/// <param name="options">Configuration options containing the polling interval for payment processing.</param>
/// <param name="logger">Logger instance for tracking worker service execution and errors.</param>
public class PaymentWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProcessingOptions> options,
    ILogger<PaymentWorkerService> logger) : BackgroundService
{
    private readonly ProcessingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Payment worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = ActivatorUtilities.CreateInstance<PaymentProcessor>(scope.ServiceProvider);
                await processor.ProcessAvailablePaymentsAsync(Guid.NewGuid().ToString("N"), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment worker iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }
}
