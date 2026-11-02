using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Tests.Unit;

public sealed class PetServiceCoverageTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task PetCrudAsync_DeveCobrirCriacaoAtualizacaoBuscaEExclusao()
    {
        await using var db = CreateDb();
        db.Tutores.Add(new Tutor { Id = 1, Nome = "Tutor" });
        await db.SaveChangesAsync();
        var service = new PetService(db);
        var dto = new PetDTO { IdTutor = 1, Nome = "Luna", ScoreSaude = 120 };

        var created = await service.CriarAsync(dto, CancellationToken.None);
        var found = await service.BuscarPorIdAsync(created!.Id, CancellationToken.None);
        var list = await service.ListarAsync(CancellationToken.None);
        var updated = await service.AtualizarAsync(created.Id, new PetDTO
        {
            IdTutor = 1,
            Nome = "Luna atualizada",
            ScoreSaude = 75
        }, CancellationToken.None);
        var missingUpdate = await service.AtualizarAsync(999, dto, CancellationToken.None);
        var deleted = await service.DeletarAsync(created.Id, CancellationToken.None);
        var missingDelete = await service.DeletarAsync(999, CancellationToken.None);

        Assert.Equal(100, found?.ScoreSaude);
        Assert.NotEmpty(list);
        Assert.True(updated);
        Assert.False(missingUpdate);
        Assert.True(deleted);
        Assert.False(missingDelete);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HistoricoEAlertasAsync_DevemCobrirTodosOsCaminhosDoRepositorio()
    {
        await using var db = CreateDb();
        db.Pets.Add(new Pet { Id = 1, Nome = "Luna", ScoreSaude = 85 });
        await db.SaveChangesAsync();
        var service = new PetService(db);
        var eventDto = new EventoSaudeDTO { Titulo = "Consulta", DataEvento = DateTime.UtcNow };
        var alertDto = new AlertaPreventivoDTO { Titulo = "Vacina", DataPrevista = DateTime.UtcNow.AddDays(2) };

        var createdEvent = await service.CriarEventoAsync(1, eventDto, CancellationToken.None);
        var createdAlert = await service.CriarAlertaAsync(1, alertDto, CancellationToken.None);
        Assert.NotNull(await service.BuscarEventoAsync(1, createdEvent!.Id, CancellationToken.None));
        Assert.NotNull(await service.BuscarAlertaAsync(1, createdAlert!.Id, CancellationToken.None));
        Assert.NotNull(await service.ListarHistoricoAsync(1, CancellationToken.None));
        Assert.NotNull(await service.ListarAlertasAsync(1, CancellationToken.None));
        Assert.True(await service.AtualizarEventoAsync(1, createdEvent.Id, eventDto, CancellationToken.None));
        Assert.True(await service.AtualizarAlertaAsync(1, createdAlert.Id, alertDto, CancellationToken.None));
        Assert.True(await service.DeletarEventoAsync(1, createdEvent.Id, CancellationToken.None));
        Assert.True(await service.DeletarAlertaAsync(1, createdAlert.Id, CancellationToken.None));

        Assert.Null(await service.ListarHistoricoAsync(999, CancellationToken.None));
        Assert.Null(await service.ListarAlertasAsync(999, CancellationToken.None));
        Assert.Null(await service.CriarEventoAsync(999, eventDto, CancellationToken.None));
        Assert.Null(await service.CriarAlertaAsync(999, alertDto, CancellationToken.None));
        Assert.Null(await service.BuscarEventoAsync(1, 999, CancellationToken.None));
        Assert.Null(await service.BuscarAlertaAsync(1, 999, CancellationToken.None));
        Assert.False(await service.AtualizarEventoAsync(1, 999, eventDto, CancellationToken.None));
        Assert.False(await service.AtualizarAlertaAsync(1, 999, alertDto, CancellationToken.None));
        Assert.False(await service.DeletarEventoAsync(1, 999, CancellationToken.None));
        Assert.False(await service.DeletarAlertaAsync(1, 999, CancellationToken.None));
    }

    [Theory]
    [InlineData(90, 0, "Baixa")]
    [InlineData(70, 0, "Media")]
    [InlineData(90, 2, "Alta")]
    [Trait("Category", "Unit")]
    public async Task GerarRecomendacaoAsync_DeveClassificarPrioridade(int score, int pendingAlerts, string expected)
    {
        await using var db = CreateDb();
        var pet = new Pet { Id = 1, Nome = "Luna", ScoreSaude = score };
        for (var i = 0; i < pendingAlerts; i++)
            pet.Alertas.Add(new AlertaPreventivo { Status = StatusAlertaComoTexto.Pendente });
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        var service = new PetService(db);

        var recommendation = await service.GerarRecomendacaoAsync(1, CancellationToken.None);

        Assert.Equal(expected, recommendation?.NivelPrioridade);
        Assert.Equal(2, recommendation?.Links.Count ?? 0);
        Assert.Null(await service.GerarRecomendacaoAsync(999, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListarAsync_DeveAplicarFiltroEOrdenacao()
    {
        await using var db = CreateDb();
        db.Tutores.AddRange(
            new Tutor { Id = 10, Nome = "Tutor 10" },
            new Tutor { Id = 20, Nome = "Tutor 20" });
        db.Pets.AddRange(
            new Pet { Id = 1, IdTutor = 10, Nome = "Zeca", ScoreSaude = 70 },
            new Pet { Id = 2, IdTutor = 10, Nome = "Amora", ScoreSaude = 95 },
            new Pet { Id = 3, IdTutor = 20, Nome = "Luna", ScoreSaude = 80 });
        await db.SaveChangesAsync();
        var service = new PetService(db);

        var result = await service.ListarAsync(CancellationToken.None, page: 1, pageSize: 1, idTutor: 10, sort: "score");

        Assert.Single(result);
        Assert.Equal("Amora", result[0].Nome);
    }

    private static OlliDb CreateDb() => new(new DbContextOptionsBuilder<OlliDb>()
        .UseInMemoryDatabase($"PetCoverageTests-{Guid.NewGuid():N}")
        .Options);
}
