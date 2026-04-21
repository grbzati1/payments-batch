namespace Payments.Domain.Entities;

public class PaymentAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public string OldStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = null!;
}
