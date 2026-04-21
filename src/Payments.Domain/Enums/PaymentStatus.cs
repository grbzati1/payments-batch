namespace Payments.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Queued = 2,
    Processing = 3,
    RetryScheduled = 4,
    Succeeded = 5,
    Failed = 6
}
