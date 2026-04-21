namespace Payments.Infrastructure.Adapters;

public sealed class FakeBankOptions
{
    public const string SectionName = "FakeBank";
    public string Mode { get; set; } = "random";
}
