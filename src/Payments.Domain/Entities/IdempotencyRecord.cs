namespace Payments.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestId { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
