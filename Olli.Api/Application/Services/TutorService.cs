using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Mappings;
using Olli.Api.Application.Repositories;
using Olli.Api.Domain.Entities;
using Olli.Api.Infrastructure.Observability;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Application.Services;

public sealed class TutorService : ITutorService
{
    private readonly IRepository<Tutor> _tutors;

    public TutorService(OlliDb db) : this(new Olli.Api.Infrastructure.Persistence.EfRepository<Tutor>(db))
    {
    }

    [ActivatorUtilitiesConstructor]
    public TutorService(IRepository<Tutor> tutors) => _tutors = tutors;

    public async Task<IReadOnlyList<TutorDTO>> ListarAsync(CancellationToken cancellationToken, int page = 1, int pageSize = 50, string? nome = null, string? sort = null)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.Listar", ActivityKind.Internal);
        var query = _tutors.Query().Include(tutor => tutor.Pets).AsQueryable();
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(tutor => tutor.Nome != null && tutor.Nome.Contains(nome));

        query = string.Equals(sort, "nome", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(tutor => tutor.Nome)
            : query.OrderBy(tutor => tutor.Id);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var tutores = await query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(cancellationToken);
        return tutores.Select(tutor => tutor.ToDto()).ToList();
    }

    public async Task<TutorDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.BuscarPorId", ActivityKind.Internal);
        var tutor = await _tutors.Query()
            .Include(item => item.Pets)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return tutor?.ToDto();
    }

    public async Task<TutorDTO> CriarAsync(TutorDTO dto, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.Criar", ActivityKind.Internal);
        var tutor = dto.ToEntity();
        await _tutors.AddAsync(tutor, cancellationToken);
        await _tutors.SaveChangesAsync(cancellationToken);
        return tutor.ToDto();
    }

    public async Task<bool> AtualizarAsync(int id, TutorDTO dto, CancellationToken cancellationToken)
    {
        var tutor = await _tutors.FindAsync([id], cancellationToken);
        if (tutor is null) return false;

        tutor.UpdateFrom(dto);
        await _tutors.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken cancellationToken)
    {
        var tutor = await _tutors.FindAsync([id], cancellationToken);
        if (tutor is null) return false;

        _tutors.Remove(tutor);
        await _tutors.SaveChangesAsync(cancellationToken);
        return true;
    }
}
