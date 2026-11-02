using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Olli.Api.Application.DTOs;

namespace Olli.Api.Tests.Integration;

public sealed class AuthenticationAndHateoasTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthenticationAndHateoasTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseName"] = $"AuthTests-{Guid.NewGuid():N}"
            }));
        }).CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Token_DeveRetornarJwt()
    {
        using var response = await _client.PostAsJsonAsync("/auth/token", new { username = "demo", password = "olli123" });
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(token.TryGetProperty("accessToken", out var accessToken));
        Assert.NotEmpty(accessToken.GetString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Consulta_DeveExporLinksHateoas()
    {
        using var response = await _client.GetAsync("/pets/1");
        var pet = await response.Content.ReadFromJsonAsync<PetDTO>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(pet);
        Assert.True(pet.Links.ContainsKey("self"));
        Assert.Equal("/pets/1", pet.Links["self"].Href);
    }
}
