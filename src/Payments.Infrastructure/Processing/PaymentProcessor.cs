using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Payments.Infrastructure.Adapters;
using Payments.Infrastructure.Persistence;

namespace Payments.Infrastructure.Processing;

public class PaymentProcessor(
    PaymentsDbContext dbContext,
    IFakeBankAdapter fakeBankAdapter,
    IOptions<ProcessingOptions> options,
    ILogger<PaymentProcessor> logger)
{
    private readonly ProcessingOptions _options = options.Value;

    public async Task<int> ProcessAvailablePaymentsAsync(string correlationId, CancellationToken cancellationToken)
    {
        await RequeueExpiredRetriesAsync(cancellationToken);
        await RequeueStaleProcessingAsync(cancellationToken);

        var payment = await TryClaimNextQueuedPaymentAsync(correlationId, cancellationToken);
        if (payment is null)
        {
            return 0;
        }

        var result = await fakeBankAdapter.ProcessAsync(payment, cancellationToken);
        var oldStatus = payment.Status.ToString();

        if (result.IsSuccess)
        {
            payment.MarkSucceeded();
            dbContext.PaymentAuditEvents.Add(CreateAudit(payment, oldStatus, payment.Status.ToString(), correlationId, result.Reason));
        }
        else if (result.IsTransient && payment.AttemptCount < _options.MaxRetryCount)
        {
            payment.ScheduleRetry(result.Code, result.Reason, DateTime.UtcNow.AddSeconds(_options.RetryDelaySeconds));
            dbContext.PaymentAuditEvents.Add(CreateAudit(payment, oldStatus, payment.Status.ToString(), correlationId, result.Reason));
        }
        else
        {
            payment.MarkFailed(result.Code, result.Reason);
            dbContext.PaymentAuditEvents.Add(CreateAudit(payment, oldStatus, payment.Status.ToString(), correlationId, result.Reason));
        }

        try
        {
            var batch = await dbContext.Batches.Include(x => x.Payments).SingleAsync(x => x.Id == payment.BatchId, cancellationToken);
            batch.RefreshStatus();

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Processed payment {PaymentId}; new status {Status}", payment.Id, payment.Status);
            return 1;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict when finalizing payment {PaymentId}. The payment will be retried or recovered by normal requeue logic.", payment.Id);
            dbContext.ChangeTracker.Clear();
            return 0;
        }
    }

    private async Task<Payment?> TryClaimNextQueuedPaymentAsync(string correlationId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .OrderBy(x => x.LastAttemptAtUtc)
            .FirstOrDefaultAsync(x => x.Status == PaymentStatus.Queued, cancellationToken);

        if (payment is null)
        {
            return null;
        }

        try
        {
            var oldStatus = payment.Status.ToString();
            payment.StartProcessing();
            dbContext.PaymentAuditEvents.Add(CreateAudit(payment, oldStatus, payment.Status.ToString(), correlationId, "Worker claimed payment"));
            await dbContext.SaveChangesAsync(cancellationToken);
            return payment;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogDebug(ex, "Skipped claimed payment {PaymentId} due to concurrency conflict.", payment.Id);
            dbContext.ChangeTracker.Clear();
            return null;
        }
    }

    private async Task RequeueExpiredRetriesAsync(CancellationToken cancellationToken)
    {
        var duePayments = await dbContext.Payments
            .Where(x => x.Status == PaymentStatus.RetryScheduled && x.RetryAfterUtc <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var payment in duePayments)
        {
            payment.QueueForProcessing();
        }

        if (duePayments.Count > 0)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogDebug(ex, "Skipped one or more retry requeue updates due to concurrency conflict.");
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task RequeueStaleProcessingAsync(CancellationToken cancellationToken)
    {
        var staleThreshold = DateTime.UtcNow.AddSeconds(-_options.StaleProcessingThresholdSeconds);
        var stalePayments = await dbContext.Payments
            .Where(x => x.Status == PaymentStatus.Processing && x.LastAttemptAtUtc < staleThreshold)
            .ToListAsync(cancellationToken);

        foreach (var payment in stalePayments)
        {
            payment.QueueForProcessing();
        }

        if (stalePayments.Count > 0)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogDebug(ex, "Skipped one or more stale processing requeue updates due to concurrency conflict.");
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private static PaymentAuditEvent CreateAudit(Payment payment, string oldStatus, string newStatus, string correlationId, string reason) => new()
    {
        PaymentId = payment.Id,
        OldStatus = oldStatus,
        NewStatus = newStatus,
        CorrelationId = correlationId,
        Reason = reason,
        OccurredAtUtc = DateTime.UtcNow
    };
}
