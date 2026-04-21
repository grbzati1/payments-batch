using Payments.Application.Commands;
using Payments.Application.Queries;

namespace Payments.Application.Services;

public interface IBatchService
{
    Task<(Guid BatchId, bool IsDuplicate)> CreateBatchAsync(CreateBatchCommand command, string requestId, CancellationToken cancellationToken);
    Task SubmitBatchAsync(SubmitBatchCommand command, string correlationId, CancellationToken cancellationToken);
    Task<BatchDto?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentDto>> GetBatchPaymentsAsync(Guid batchId, CancellationToken cancellationToken);
    Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
}
