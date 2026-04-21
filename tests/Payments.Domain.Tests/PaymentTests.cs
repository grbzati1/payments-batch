using FluentAssertions;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Xunit;

namespace Payments.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void Payment_Should_Require_Positive_Amount()
    {
        var action = () => new Payment("PAY-001", "USD", 0m, "Acme", "US123");
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Payment_Should_Move_To_Succeeded()
    {
        var payment = new Payment("PAY-001", "USD", 10m, "Acme", "US123");
        payment.QueueForProcessing();
        payment.StartProcessing();
        payment.MarkSucceeded();

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.IsTerminal.Should().BeTrue();
    }
}
