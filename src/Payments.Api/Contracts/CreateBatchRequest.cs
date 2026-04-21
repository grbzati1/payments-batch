namespace Payments.Api.Contracts;

/// <summary>
/// Represents a request to create a batch of payments.
/// </summary>
public sealed class CreateBatchRequest
{
    /// <summary>
    /// Gets or sets the client's unique reference identifier for this payment batch.
    /// </summary>
    public string ClientBatchReference { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of payment requests to be included in this batch.
    /// </summary>
    public List<CreatePaymentRequest> Payments { get; set; } = [];
}
