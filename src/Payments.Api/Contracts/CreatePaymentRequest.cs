namespace Payments.Api.Contracts;

/// <summary>
/// Represents a request to create a payment with the required details for processing.
/// </summary>
/// <remarks>This class encapsulates the information needed to initiate a payment, including the payment
/// reference, currency, amount, beneficiary name, and destination account. All properties must be set with valid values
/// before submitting the request. This type is typically used as a data transfer object when interacting with payment
/// processing APIs.</remarks>
public sealed class CreatePaymentRequest
{
    public string ClientPaymentReference { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public decimal Amount { get; set; }
    public string BeneficiaryName { get; set; } = null!;
    public string DestinationAccount { get; set; } = null!;
}

