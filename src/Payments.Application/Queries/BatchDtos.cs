namespace Payments.Application.Queries;

public sealed record BatchDto(
    Guid Id,
    string ClientBatchReference,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    IReadOnlyCollection<PaymentDto> Payments);

public sealed record PaymentDto(
    Guid Id,
    Guid BatchId,
    string ClientPaymentReference,
    string Currency,
    decimal Amount,
    string BeneficiaryName,
    string DestinationAccount,
    string Status,
    string? FailureCode,
    string? FailureReason,
    int AttemptCount,
    DateTime? LastAttemptAtUtc);
