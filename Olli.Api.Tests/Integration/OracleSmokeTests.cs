using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit.Sdk;

namespace Olli.Api.Tests.Integration;

public sealed class OracleSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public OracleSmokeTests(WebApplicationFactory<Program> baseFactory)
    {
        _baseFactory = baseFactory;
    }

    [SkippableFact]
    [Trait("Category", "Oracle")]
    public async Task Oracle_DeveResponderAoHealthCheck()
    {
        // Arrange
        var connectionString = Environment.GetEnvironmentVariable("ORACLE_TEST_CONNECTION_STRING");
        Skip.If(string.IsNullOrWhiteSpace(connectionString),
            "Defina ORACLE_TEST_CONNECTION_STRING para executar o smoke test Oracle.");

        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("OracleIntegration");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "Oracle",
                    ["ConnectionStrings:FiapOracle"] = connectionString,
                    ["SeedDatabase"] = "false",
                    ["ApplyMigrationsOnStartup"] = "false",
                    ["ExternalServices:Urls:fiap"] = ""
                }));
        });
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/database");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var oracleStatus = document.RootElement
            .GetProperty("entries")
            .GetProperty("oracle-fiap")
            .GetProperty("status")
            .GetString();
        Assert.Equal("Healthy", oracleStatus);
    }
}
