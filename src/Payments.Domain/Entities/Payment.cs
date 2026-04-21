using Payments.Domain.Enums;

namespace Payments.Domain.Entities;

public class Payment
{
    private Payment() { }

    public Payment(
        string clientPaymentReference,
        string currency,
        decimal amount,
        string beneficiaryName,
        string destinationAccount)
    {
        if (string.IsNullOrWhiteSpace(clientPaymentReference))
            throw new ArgumentException("Client payment reference is required.", nameof(clientPaymentReference));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        if (string.IsNullOrWhiteSpace(beneficiaryName))
            throw new ArgumentException("Beneficiary name is required.", nameof(beneficiaryName));
        if (string.IsNullOrWhiteSpace(destinationAccount))
            throw new ArgumentException("Destination account is required.", nameof(destinationAccount));

        Id = Guid.NewGuid();
        ClientPaymentReference = clientPaymentReference.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        Amount = amount;
        BeneficiaryName = beneficiaryName.Trim();
        DestinationAccount = destinationAccount.Trim();
        Status = PaymentStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string ClientPaymentReference { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string BeneficiaryName { get; private set; } = null!;
    public string DestinationAccount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? RetryAfterUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public bool IsTerminal => Status is PaymentStatus.Succeeded or PaymentStatus.Failed;

    public void SetBatchId(Guid batchId) => BatchId = batchId;

    public void QueueForProcessing()
    {
        if (IsTerminal)
            throw new InvalidOperationException("Terminal payments cannot be re-queued.");

        Status = PaymentStatus.Queued;
        FailureCode = null;
        FailureReason = null;
        RetryAfterUtc = null;
    }

    public void StartProcessing()
    {
        if (Status is not PaymentStatus.Queued)
            throw new InvalidOperationException("Only queued payments can start processing.");

        Status = PaymentStatus.Processing;
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
    }

    public void MarkSucceeded()
    {
        if (IsTerminal)
            throw new InvalidOperationException("Terminal payments cannot be changed.");

        Status = PaymentStatus.Succeeded;
        FailureCode = null;
        FailureReason = null;
        RetryAfterUtc = null;
    }

    public void ScheduleRetry(string failureCode, string failureReason, DateTime retryAfterUtc)
    {
        if (IsTerminal)
            throw new InvalidOperationException("Terminal payments cannot be retried.");

        Status = PaymentStatus.RetryScheduled;
        FailureCode = failureCode;
        FailureReason = failureReason;
        RetryAfterUtc = retryAfterUtc;
    }

    public void MarkFailed(string failureCode, string failureReason)
    {
        if (IsTerminal)
            throw new InvalidOperationException("Terminal payments cannot be changed.");

        Status = PaymentStatus.Failed;
        FailureCode = failureCode;
        FailureReason = failureReason;
        RetryAfterUtc = null;
    }
}
