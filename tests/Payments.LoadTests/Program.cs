using System.Net.Http.Json;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;

var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:8080";
var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

var scenario = Scenario.Create("create_submit", async context =>
{
    var requestId = Guid.NewGuid().ToString();

    var body = new
    {
        clientBatchReference = $"BATCH-{Guid.NewGuid()}",
        payments = new[]
        {
            new
            {
                clientPaymentReference = $"PAY-{Guid.NewGuid()}",
                currency = "USD",
                amount = 10.25,
                beneficiaryName = "Acme Ltd",
                destinationAccount = "US123"
            }
        }
    };

    string? batchId = null;

    // CREATE
    var create = await Step.Run("create", context, async () =>
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/batches");
        req.Headers.Add("X-Request-Id", requestId);
        req.Content = JsonContent.Create(body);

        var res = await http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return Response.Fail();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        batchId = json.RootElement.GetProperty("batchId").GetString();

        return string.IsNullOrEmpty(batchId) ? Response.Fail() : Response.Ok();
    });

    if (create.IsError || batchId is null)
        return Response.Fail();

    // SUBMIT
    var submit = await Step.Run("submit", context, async () =>
    {
        var res = await http.PostAsync($"/api/batches/{batchId}/submit", null);
        return res.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });

    return !submit.IsError ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();