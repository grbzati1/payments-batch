using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Payments.Api;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Payments.IntegrationTests;

public class BatchesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };


    public BatchesApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Batch_Should_Return_Created()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/batches", new
        {
            clientBatchReference = "BATCH-001",
            payments = new[]
            {
                new
                {
                    clientPaymentReference = "PAY-001",
                    currency = "USD",
                    amount = 10.25m,
                    beneficiaryName = "Acme Ltd",
                    destinationAccount = "US123"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBatch_WithSameRequestId_IsIdempotent()
    {
        var requestId = Guid.NewGuid().ToString();

        var body = new
        {
            clientBatchReference = $"BATCH-{Guid.NewGuid():N}",
            payments = new[]
            {
            new
            {
                clientPaymentReference = $"PAY-{Guid.NewGuid():N}",
                currency = "USD",
                amount = 10.25m,
                beneficiaryName = "Acme Ltd",
                destinationAccount = "ACC-SUCCESS"
            }
        }
        };

        async Task<CreateBatchResponse?> SendCreate()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/batches")
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add("X-Request-Id", requestId);

            var res = await _client.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<CreateBatchResponse>(_json);
        }

        var first = await SendCreate();
        var second = await SendCreate();

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.BatchId.Should().Be(second!.BatchId);
    }


    private sealed class CreateBatchResponse
    {
        public string? BatchId { get; set; }
        public string? Status { get; set; }
    }

}
