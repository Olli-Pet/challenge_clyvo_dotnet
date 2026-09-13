using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Mappings;
using Olli.Api.Infrastructure.Observability;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Application.Services;

public sealed class TutorService(OlliDb db) : ITutorService
{
    public async Task<IReadOnlyList<TutorDTO>> ListarAsync(CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.Listar", ActivityKind.Internal);
        var tutores = await db.Tutores
            .Include(tutor => tutor.Pets)
            .ToListAsync(cancellationToken);
        return tutores.Select(tutor => tutor.ToDto()).ToList();
    }

    public async Task<TutorDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.BuscarPorId", ActivityKind.Internal);
        var tutor = await db.Tutores
            .Include(item => item.Pets)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return tutor?.ToDto();
    }

    public async Task<TutorDTO> CriarAsync(TutorDTO dto, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("TutorService.Criar", ActivityKind.Internal);
        var tutor = dto.ToEntity();
        db.Tutores.Add(tutor);
        await db.SaveChangesAsync(cancellationToken);
        return tutor.ToDto();
    }

    public async Task<bool> AtualizarAsync(int id, TutorDTO dto, CancellationToken cancellationToken)
    {
        var tutor = await db.Tutores.FindAsync([id], cancellationToken);
        if (tutor is null) return false;

        tutor.UpdateFrom(dto);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken cancellationToken)
    {
        var tutor = await db.Tutores.FindAsync([id], cancellationToken);
        if (tutor is null) return false;

        db.Tutores.Remove(tutor);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
