using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Fundo.Infrastructure.Tests;

public sealed class ApplicationEndpointTests : IClassFixture<ApplicationApiFactory>
{
    private readonly HttpClient _client;

    public ApplicationEndpointTests(ApplicationApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Submit_returns_the_documented_http_contracts()
    {
        var approved = await Submit(new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "CA", companyName = "Fundo", requestedAmount = 100m, ssn = "123-45-6789" });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using (var body = JsonDocument.Parse(await approved.Content.ReadAsStringAsync()))
        {
            Assert.Equal("approved", body.RootElement.GetProperty("decision").GetString());
            Assert.True(Guid.TryParse(body.RootElement.GetProperty("applicationId").GetString(), out _));
        }

        var stateDenied = await Submit(new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "NY", companyName = "Fundo", requestedAmount = 100m, ssn = "234-56-7890" });
        var blacklisted = await Submit(new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "CA", companyName = "Fundo", requestedAmount = 100m, ssn = "111-22-3333" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stateDenied.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blacklisted.StatusCode);
        Assert.Contains("state-not-supported", await stateDenied.Content.ReadAsStringAsync());
        Assert.Contains("ssn-blacklisted", await blacklisted.Content.ReadAsStringAsync());

        foreach (var invalid in new object[]
        {
            new { lastName = "Lovelace", address = "1 Main St", state = "CA", companyName = "Fundo", requestedAmount = 100m, ssn = "123-45-6789" },
            new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "ZZ", companyName = "Fundo", requestedAmount = 100m, ssn = "123-45-6789" },
            new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "CA", companyName = "Fundo", requestedAmount = 0m, ssn = "123-45-6789" },
            new { firstName = "Ada", lastName = "Lovelace", address = "1 Main St", state = "CA", companyName = "Fundo", requestedAmount = 100m, ssn = "123-45-678" }
        })
        {
            var response = await Submit(invalid);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("errors", await response.Content.ReadAsStringAsync());
        }
    }

    private Task<HttpResponseMessage> Submit(object request) =>
        _client.PostAsJsonAsync("/api/applications", request);
}

public sealed class ApplicationApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"fundo-api-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, configuration) =>
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Fundo"] = $"Data Source={_databasePath}",
            ["ExternalServiceUrl"] = "http://127.0.0.1:1/"
        }));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
                File.Delete(path);
    }
}
