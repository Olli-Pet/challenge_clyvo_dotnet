using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Domain.Entities;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Tests.Unit;

public sealed class TutorServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListarEBuscarAsync_DevemRetornarTutorComQuantidadeDePets()
    {
        await using var db = CreateDb();
        db.Tutores.Add(new Tutor { Id = 1, Nome = "Ana", Pets = [new Pet { Nome = "Luna" }] });
        await db.SaveChangesAsync();
        var service = new TutorService(db);

        var list = await service.ListarAsync(CancellationToken.None);
        var found = await service.BuscarPorIdAsync(1, CancellationToken.None);
        var missing = await service.BuscarPorIdAsync(999, CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(1, list[0].QuantidadePets);
        Assert.Equal("Ana", found?.Nome);
        Assert.Null(missing);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CriarAtualizarEDeletarAsync_DevemPersistirMudancas()
    {
        await using var db = CreateDb();
        var service = new TutorService(db);
        var created = await service.CriarAsync(new TutorDTO { Nome = "Bruno", Email = "bruno@email.com" }, CancellationToken.None);

        Assert.True(created.Id > 0);
        Assert.True(await service.AtualizarAsync(created.Id, new TutorDTO { Nome = "Bruno Atualizado" }, CancellationToken.None));
        Assert.False(await service.AtualizarAsync(999, new TutorDTO { Nome = "Não existe" }, CancellationToken.None));
        Assert.True(await service.DeletarAsync(created.Id, CancellationToken.None));
        Assert.False(await service.DeletarAsync(999, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListarAsync_DeveAplicarFiltroOrdenacaoEPaginacao()
    {
        await using var db = CreateDb();
        db.Tutores.AddRange(
            new Tutor { Nome = "Carlos" },
            new Tutor { Nome = "Ana" },
            new Tutor { Nome = "Bruna" });
        await db.SaveChangesAsync();
        var service = new TutorService(db);

        var result = await service.ListarAsync(CancellationToken.None, page: 1, pageSize: 1, nome: "a", sort: "nome");

        Assert.Single(result);
        Assert.Equal("Ana", result[0].Nome);
    }

    private static OlliDb CreateDb() => new(new DbContextOptionsBuilder<OlliDb>()
        .UseInMemoryDatabase($"TutorTests-{Guid.NewGuid():N}")
        .Options);
}
