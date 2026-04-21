using Microsoft.EntityFrameworkCore;
using Payments.Infrastructure;
using Payments.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Payments.Api;


/// <summary>
/// Configures and runs the Payments API web application.
/// </summary>
/// <remarks>This entry point sets up essential services, middleware, and endpoints for the Payments API,
/// including controllers, health checks, and OpenTelemetry for observability. It also
/// ensures the database is created before the application starts accepting requests. The application exposes health
/// check endpoints for liveness and readiness, and provides a simple health status endpoint at '/health'.</remarks>
public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddPaymentsInfrastructure(builder.Configuration);

        // OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "Payments.Api",
                serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter());

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.Run();
    }
}