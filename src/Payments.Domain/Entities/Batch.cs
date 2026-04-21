using Payments.Domain.Enums;

namespace Payments.Domain.Entities;

public class Batch
{
    private readonly List<Payment> _payments = [];

    private Batch() { }

    public Batch(string clientBatchReference, IEnumerable<Payment> payments)
    {
        if (string.IsNullOrWhiteSpace(clientBatchReference))
            throw new ArgumentException("Client batch reference is required.", nameof(clientBatchReference));

        var paymentList = payments?.ToList() ?? [];
        if (paymentList.Count == 0)
            throw new ArgumentException("At least one payment is required.", nameof(payments));

        Id = Guid.NewGuid();
        ClientBatchReference = clientBatchReference.Trim();
        Status = BatchStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        _payments.AddRange(paymentList);
    }

    public Guid Id { get; private set; }
    public string ClientBatchReference { get; private set; } = null!;
    public BatchStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public List<Payment> Payments => _payments;

    public void Submit()
    {
        if (Status != BatchStatus.Draft)
            throw new InvalidOperationException("Only draft batches can be submitted.");

        foreach (var payment in _payments)
        {
            payment.QueueForProcessing();
        }

        Status = BatchStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void RefreshStatus()
    {
        if (_payments.Count == 0)
        {
            Status = BatchStatus.Draft;
            return;
        }

        if (_payments.All(x => x.IsTerminal))
        {
            Status = _payments.Any(x => x.Status == PaymentStatus.Failed)
                ? BatchStatus.CompletedWithFailures
                : BatchStatus.Completed;
            return;
        }

        if (_payments.Any(x => x.Status is PaymentStatus.Processing or PaymentStatus.Queued or PaymentStatus.RetryScheduled))
        {
            Status = BatchStatus.Processing;
        }
    }
}
