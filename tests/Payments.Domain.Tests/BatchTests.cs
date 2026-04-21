using FluentAssertions;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Xunit;

namespace Payments.Domain.Tests;

public class BatchTests
{
    [Fact]
    public void Submit_Should_Queue_All_Payments()
    {
        var batch = new Batch("BATCH-001", [
            new Payment("PAY-001", "USD", 10m, "Acme", "US123"),
            new Payment("PAY-002", "GBP", 20m, "Beta", "GB123")
        ]);

        batch.Submit();
        batch.RefreshStatus();

        batch.Status.Should().Be(BatchStatus.Processing);
        batch.Payments.Should().OnlyContain(x => x.Status == PaymentStatus.Queued);
    }
}
