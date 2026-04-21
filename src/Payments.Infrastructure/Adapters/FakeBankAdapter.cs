using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Domain.Entities;

namespace Payments.Infrastructure.Adapters;

public class FakeBankAdapter(IOptions<FakeBankOptions> options, ILogger<FakeBankAdapter> logger) : IFakeBankAdapter
{
    private readonly FakeBankOptions _options = options.Value;

    public Task<FakeBankResult> ProcessAsync(Payment payment, CancellationToken cancellationToken)
    {
        var mode = _options.Mode.Trim().ToLowerInvariant();
        logger.LogInformation("Processing payment {PaymentId} with fake bank mode {Mode}", payment.Id, mode);

        return Task.FromResult(mode switch
        {
            "success" => new FakeBankResult(true, false, "OK", "Accepted by fake bank"),
            "transient" => new FakeBankResult(false, true, "TIMEOUT", "Transient bank timeout"),
            "permanent" => new FakeBankResult(false, false, "REJECTED", "Permanent bank rejection"),
            _ => CreateRandomResult(payment)
        });
    }

    private static FakeBankResult CreateRandomResult(Payment payment)
    {
        var seed = HashCode.Combine(payment.Id, payment.AttemptCount, DateTime.UtcNow.Second);
        var value = Math.Abs(seed % 100);

        if (value < 65) return new FakeBankResult(true, false, "OK", "Accepted by fake bank");
        if (value < 85) return new FakeBankResult(false, true, "TIMEOUT", "Transient bank timeout");
        return new FakeBankResult(false, false, "REJECTED", "Permanent bank rejection");
    }
}
