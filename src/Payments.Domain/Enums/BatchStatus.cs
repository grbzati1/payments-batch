namespace Payments.Domain.Enums;

public enum BatchStatus
{
    Draft = 1,
    Submitted = 2,
    Processing = 3,
    Completed = 4,
    CompletedWithFailures = 5
}
