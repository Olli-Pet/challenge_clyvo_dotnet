using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Olli.Api.Application.DTOs;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Tests;

public sealed class OlliApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OlliApiTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = $"OlliApiTests-{Guid.NewGuid():N}";
        _factory = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DatabaseName"] = databaseName
                    }));
            });
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/database")]
    [InlineData("/health/external")]
    [Trait("Category", "Integration")]
    public async Task HealthChecks_DevemRetornarStatusOk(string endpoint)
    {
        // Arrange
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CorrelationId_InformadoDeveSerDevolvidoNaResposta()
    {
        // Arrange
        const string expectedCorrelationId = "test-correlation-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedCorrelationId, response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Metrics_DeveRetornarFormatoPrometheus()
    {
        // Arrange
        // Act
        using var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("# TYPE olli_api_requests_total counter", content);
        Assert.Contains("olli_api_request_duration_ms_count", content);
        Assert.Contains("olli_api_request_duration_ms_bucket{le=\"+Inf\"}", content);
        Assert.Contains("X-Correlation-ID", response.Headers.Select(header => header.Key));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListarPets_DeveRetornar200OkComLista()
    {
        // Arrange
        // Act
        using var response = await _client.GetAsync("/pets");
        var pets = await response.Content.ReadFromJsonAsync<List<PetDTO>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(pets);
        Assert.NotEmpty(pets);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CriarPet_SemIdempotencyKey_DeveRetornar400BadRequest()
    {
        // Arrange
        var pet = CreatePet();

        // Act
        using var response = await _client.PostAsJsonAsync("/pets", pet);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("IdempotencyKey", content);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CriarPet_ComIdempotencyKey_DeveRetornar201Created()
    {
        // Arrange
        var pet = CreatePet();
        using var request = CreatePostRequest("/pets", pet);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<PetDTO>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CriarTutor_ComIdempotencyKey_DeveRetornar201Created()
    {
        // Arrange
        var tutor = new TutorDTO
        {
            Nome = "Ana Souza",
            Email = "ana.souza@example.com",
            Telefone = "+55 11 98888-0000"
        };
        using var request = CreatePostRequest("/tutores", tutor);

        // Act
        using var response = await _client.SendAsync(request);
        var createdTutor = await response.Content.ReadFromJsonAsync<TutorDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdTutor);
        Assert.Equal("Ana Souza", createdTutor.Nome);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CriarTutor_SemIdempotencyKey_DeveRetornar400BadRequest()
    {
        // Arrange
        var tutor = new TutorDTO { Nome = "Tutor sem chave" };

        // Act
        using var response = await _client.PostAsJsonAsync("/tutores", tutor);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Tutores_DevePermitirCrudCompleto()
    {
        // Arrange
        var tutor = new TutorDTO
        {
            Nome = "Tutor CRUD",
            Email = "tutor.crud@example.com",
            Telefone = "+55 11 97777-1111"
        };
        using var createRequest = CreatePostRequest("/tutores", tutor);

        // Act
        using var listResponse = await _client.GetAsync("/tutores");
        using var createResponse = await _client.SendAsync(createRequest);
        var createdTutor = await createResponse.Content.ReadFromJsonAsync<TutorDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdTutor);

        using var getResponse = await _client.GetAsync($"/tutores/{createdTutor.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        createdTutor.Nome = "Tutor CRUD atualizado";
        using var updateResponse = await _client.PutAsJsonAsync($"/tutores/{createdTutor.Id}", createdTutor);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var deleteResponse = await _client.DeleteAsync($"/tutores/{createdTutor.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedResponse = await _client.GetAsync($"/tutores/{createdTutor.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pets_DevePermitirAtualizacaoEExclusao()
    {
        // Arrange
        using var createRequest = CreatePostRequest("/pets", CreatePet());

        // Act
        using var createResponse = await _client.SendAsync(createRequest);
        var createdPet = await createResponse.Content.ReadFromJsonAsync<PetDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdPet);

        createdPet.Nome = "Nina atualizada";
        createdPet.ScoreSaude = 95;
        using var updateResponse = await _client.PutAsJsonAsync($"/pets/{createdPet.Id}", createdPet);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var deleteResponse = await _client.DeleteAsync($"/pets/{createdPet.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedResponse = await _client.GetAsync($"/pets/{createdPet.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_InMemory_DeveInformarChecksEsperados()
    {
        // Arrange
        // Act
        using var response = await _client.GetAsync("/health");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = document.RootElement.GetProperty("entries");
        Assert.Equal("Healthy", entries.GetProperty("api").GetProperty("status").GetString());
        Assert.Equal("Healthy", entries.GetProperty("in-memory-db").GetProperty("status").GetString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecursosInexistentes_DevemRetornar404()
    {
        // Arrange
        var requests = new[]
        {
            _client.GetAsync("/tutores/-9999"),
            _client.GetAsync("/pets/9999/historico"),
            _client.GetAsync("/pets/9999/alertas"),
            _client.GetAsync("/pets/9999/recomendacao-ia"),
            _client.GetAsync("/pets/1/historico/9999"),
            _client.PutAsJsonAsync("/pets/1/historico/9999", new EventoSaudeDTO()),
            _client.DeleteAsync("/pets/1/historico/9999"),
            _client.GetAsync("/pets/1/alertas/9999"),
            _client.PutAsJsonAsync("/pets/1/alertas/9999", new AlertaPreventivoDTO()),
            _client.DeleteAsync("/pets/1/alertas/9999")
        };

        // Act
        var responses = await Task.WhenAll(requests);

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        foreach (var response in responses)
            response.Dispose();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OperacoesDeAlteracaoEmRecursosInexistentes_DevemRetornar404()
    {
        // Arrange
        // Act
        using var updateTutor = await _client.PutAsJsonAsync("/tutores/-9999", new TutorDTO { Nome = "Inexistente" });
        using var deleteTutor = await _client.DeleteAsync("/tutores/-9999");
        using var updatePet = await _client.PutAsJsonAsync("/pets/-9999", CreatePet());
        using var deletePet = await _client.DeleteAsync("/pets/-9999");
        using var updateEvent = await _client.PutAsJsonAsync("/pets/1/historico/9999", new EventoSaudeDTO());
        using var deleteEvent = await _client.DeleteAsync("/pets/1/historico/9999");
        using var updateAlert = await _client.PutAsJsonAsync("/pets/1/alertas/9999", new AlertaPreventivoDTO());
        using var deleteAlert = await _client.DeleteAsync("/pets/1/alertas/9999");

        // Assert
        Assert.All(
            new[] { updateTutor, deleteTutor, updatePet, deletePet, updateEvent, deleteEvent, updateAlert, deleteAlert },
            response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(-9999, HttpStatusCode.NotFound)]
    [Trait("Category", "Integration")]
    public async Task BuscarPetPorId_DeveRetornarStatusCodeCorreto(int id, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        // Act
        using var response = await _client.GetAsync($"/pets/{id}");

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GerarRecomendacaoIa_DeveRetornar200Ok()
    {
        // Arrange
        // Act
        using var response = await _client.GetAsync("/pets/1/recomendacao-ia");
        var recommendation = await response.Content.ReadFromJsonAsync<RecomendacaoIaDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(recommendation);
        Assert.Equal(1, recommendation.IdPet);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HistoricoSaude_DevePermitirCrudCompleto()
    {
        // Arrange
        var evento = new EventoSaudeDTO
        {
            Tipo = TipoEventoSaudeComoTexto.Consulta,
            Titulo = "Consulta preventiva",
            Descricao = "Consulta de rotina para acompanhamento.",
            DataEvento = new DateTime(2026, 5, 18),
            NomeVeterinario = "Dra. Clyvo",
            PrecisaRetorno = true,
            DataRetornoSugerida = new DateTime(2026, 6, 18)
        };
        using var createRequest = CreatePostRequest("/pets/1/historico", evento);

        // Act
        using var createResponse = await _client.SendAsync(createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventoSaudeDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdEvent);

        using var getResponse = await _client.GetAsync($"/pets/1/historico/{createdEvent.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var listResponse = await _client.GetAsync("/pets/1/historico");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        createdEvent.Titulo = "Consulta preventiva atualizada";
        createdEvent.PrecisaRetorno = false;
        using var updateResponse = await _client.PutAsJsonAsync($"/pets/1/historico/{createdEvent.Id}", createdEvent);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var deleteResponse = await _client.DeleteAsync($"/pets/1/historico/{createdEvent.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedResponse = await _client.GetAsync($"/pets/1/historico/{createdEvent.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AlertasPreventivos_DevePermitirCrudCompleto()
    {
        // Arrange
        var alerta = new AlertaPreventivoDTO
        {
            Tipo = TipoAlertaComoTexto.Vacina,
            Titulo = "Reforco de vacina",
            Descricao = "Verificar carteira de vacinas.",
            DataPrevista = new DateTime(2026, 6, 30),
            Status = StatusAlertaComoTexto.Pendente,
            RecomendacaoIa = "Enviar lembrete ao tutor."
        };
        using var createRequest = CreatePostRequest("/pets/1/alertas", alerta);

        // Act
        using var createResponse = await _client.SendAsync(createRequest);
        var createdAlert = await createResponse.Content.ReadFromJsonAsync<AlertaPreventivoDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdAlert);

        using var getResponse = await _client.GetAsync($"/pets/1/alertas/{createdAlert.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var listResponse = await _client.GetAsync("/pets/1/alertas");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        createdAlert.Titulo = "Reforco de vacina atualizado";
        createdAlert.Status = StatusAlertaComoTexto.Concluido;
        using var updateResponse = await _client.PutAsJsonAsync($"/pets/1/alertas/{createdAlert.Id}", createdAlert);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var deleteResponse = await _client.DeleteAsync($"/pets/1/alertas/{createdAlert.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedResponse = await _client.GetAsync($"/pets/1/alertas/{createdAlert.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListarPets_MuitasVezes_DeveAcionarRateLimitCom429()
    {
        // Arrange
        var requests = Enumerable.Range(0, 12)
            .Select(_ => _client.GetAsync("/pets"));

        // Act
        var responses = await Task.WhenAll(requests);

        // Assert
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.TooManyRequests);
        foreach (var response in responses)
            response.Dispose();
    }

    private static PetDTO CreatePet() => new()
    {
        IdTutor = 1,
        Nome = "Nina",
        Especie = EspeciePetComoTexto.Gato,
        Raca = "SRD",
        DataNascimento = new DateTime(2021, 3, 12),
        PesoKg = 4.2m,
        ScoreSaude = 88
    };

    private static HttpRequestMessage CreatePostRequest<T>(string path, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("IdempotencyKey", Guid.NewGuid().ToString("N"));
        return request;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
