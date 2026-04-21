using Microsoft.EntityFrameworkCore;
using Payments.Infrastructure;
using Payments.Infrastructure.Persistence;
using Payments.Worker.Services;

namespace Payments.Worker;

/// <summary>
/// Entry point for the Payments Worker Service that processes payment-related background tasks.
/// Configures the host application, registers services, ensures database initialization, and starts the worker service.
/// </summary>
public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddPaymentsInfrastructure(builder.Configuration);
        builder.Services.AddHostedService<PaymentWorkerService>();

        var host = builder.Build();
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}