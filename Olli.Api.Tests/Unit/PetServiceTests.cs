using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Tests.Unit;

public sealed class PetServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CriarAsync_ComTutorInexistenteDeveRetornarNulo()
    {
        // Arrange
        await using var db = CreateDb();
        var service = new PetService(db);
        var dto = new PetDTO { IdTutor = 404, Nome = "Luna" };

        // Act
        var result = await service.CriarAsync(dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Empty(db.Pets);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GerarRecomendacaoAsync_ComScoreBaixoDeveRetornarPrioridadeAlta()
    {
        // Arrange
        await using var db = CreateDb();
        db.Pets.Add(new Pet
        {
            Id = 1,
            IdTutor = 1,
            Nome = "Luna",
            ScoreSaude = 40,
            Especie = EspeciePetComoTexto.Gato
        });
        await db.SaveChangesAsync();
        var service = new PetService(db);

        // Act
        var result = await service.GerarRecomendacaoAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alta", result.NivelPrioridade);
        Assert.Contains("score 40", result.Resumo);
        Assert.Equal(3, result.ProximasAcoes.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListarHistoricoAsync_DeveOrdenarEventosDoMaisRecenteParaOMaisAntigo()
    {
        // Arrange
        await using var db = CreateDb();
        db.Pets.Add(new Pet { Id = 1, Nome = "Luna" });
        db.EventosSaude.AddRange(
            new EventoSaude { IdPet = 1, Titulo = "Antigo", DataEvento = new DateTime(2025, 1, 1) },
            new EventoSaude { IdPet = 1, Titulo = "Recente", DataEvento = new DateTime(2026, 1, 1) });
        await db.SaveChangesAsync();
        var service = new PetService(db);

        // Act
        var result = await service.ListarHistoricoAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            item => Assert.Equal("Recente", item.Titulo),
            item => Assert.Equal("Antigo", item.Titulo));
    }

    private static OlliDb CreateDb() => new(new DbContextOptionsBuilder<OlliDb>()
        .UseInMemoryDatabase($"UnitTests-{Guid.NewGuid():N}")
        .Options);
}
