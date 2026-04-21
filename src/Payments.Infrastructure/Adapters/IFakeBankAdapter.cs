using Payments.Domain.Entities;

namespace Payments.Infrastructure.Adapters;

public interface IFakeBankAdapter
{
    Task<FakeBankResult> ProcessAsync(Payment payment, CancellationToken cancellationToken);
}

public sealed record FakeBankResult(bool IsSuccess, bool IsTransient, string Code, string Reason);
