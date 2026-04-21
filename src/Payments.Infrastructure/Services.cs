using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payments.Application.Commands;
using Payments.Application.Queries;
using Payments.Application.Services;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Payments.Infrastructure.Adapters;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Processing;

namespace Payments.Infrastructure;

public static class Services
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentsDb") ?? "Data Source=/data/payments.db";

        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(connectionString));
        services.Configure<ProcessingOptions>(configuration.GetSection(ProcessingOptions.SectionName));
        services.Configure<FakeBankOptions>(configuration.GetSection(FakeBankOptions.SectionName));
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IFakeBankAdapter, FakeBankAdapter>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("payments-batch"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("payments-batch")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}

internal sealed class BatchService(PaymentsDbContext dbContext) : IBatchService
{
    public async Task<(Guid BatchId, bool IsDuplicate)> CreateBatchAsync(CreateBatchCommand command, string requestId, CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existing = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);

            if (existing is not null)
            {
                await tx.CommitAsync(cancellationToken);
                return (existing.ResourceId, true);
            }

            var payments = command.Payments.Select(x => new Payment(
                x.ClientPaymentReference,
                x.Currency,
                x.Amount,
                x.BeneficiaryName,
                x.DestinationAccount)).ToList();

            var batch = new Batch(command.ClientBatchReference, payments);
            foreach (var payment in payments)
            {
                payment.SetBatchId(batch.Id);
            }

            dbContext.Batches.Add(batch);
            dbContext.IdempotencyRecords.Add(new IdempotencyRecord
            {
                RequestId = requestId,
                ResourceId = batch.Id,
                ResourceType = nameof(Batch)
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return (batch.Id, false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SubmitBatchAsync(SubmitBatchCommand command, string correlationId, CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var batch = await dbContext.Batches.Include(x => x.Payments)
                .SingleOrDefaultAsync(x => x.Id == command.BatchId, cancellationToken)
                ?? throw new KeyNotFoundException($"Batch {command.BatchId} not found.");

            batch.Submit();

            foreach (var payment in batch.Payments)
            {
                dbContext.PaymentAuditEvents.Add(new PaymentAuditEvent
                {
                    PaymentId = payment.Id,
                    OldStatus = PaymentStatus.Pending.ToString(),
                    NewStatus = payment.Status.ToString(),
                    CorrelationId = correlationId,
                    Reason = "Batch submitted"
                });
            }

            batch.RefreshStatus();
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BatchDto?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.Batches.Include(x => x.Payments)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);

        return batch is null ? null : MapBatch(batch);
    }

    public async Task<IReadOnlyCollection<PaymentDto>> GetBatchPaymentsAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.ClientPaymentReference)
            .Select(MapPaymentExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.Id == paymentId)
            .Select(MapPaymentExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static BatchDto MapBatch(Batch batch) => new(
        batch.Id,
        batch.ClientBatchReference,
        batch.Status.ToString(),
        batch.CreatedAtUtc,
        batch.SubmittedAtUtc,
        batch.Payments.Select(x => new PaymentDto(
            x.Id,
            x.BatchId,
            x.ClientPaymentReference,
            x.Currency,
            x.Amount,
            x.BeneficiaryName,
            x.DestinationAccount,
            x.Status.ToString(),
            x.FailureCode,
            x.FailureReason,
            x.AttemptCount,
            x.LastAttemptAtUtc)).ToList());

    private static System.Linq.Expressions.Expression<Func<Payment, PaymentDto>> MapPaymentExpression() => x => new PaymentDto(
        x.Id,
        x.BatchId,
        x.ClientPaymentReference,
        x.Currency,
        x.Amount,
        x.BeneficiaryName,
        x.DestinationAccount,
        x.Status.ToString(),
        x.FailureCode,
        x.FailureReason,
        x.AttemptCount,
        x.LastAttemptAtUtc);
}
