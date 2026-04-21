namespace Payments.Application.Commands;

public sealed record CreateBatchCommand(string ClientBatchReference, IReadOnlyCollection<CreatePaymentItem> Payments);

public sealed record CreatePaymentItem(
    string ClientPaymentReference,
    string Currency,
    decimal Amount,
    string BeneficiaryName,
    string DestinationAccount);
