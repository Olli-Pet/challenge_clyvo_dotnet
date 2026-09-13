using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Mappings;
using Olli.Api.Domain.Enums;
using Olli.Api.Infrastructure.Observability;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Application.Services;

public sealed class PetService(OlliDb db) : IPetService
{
    public async Task<IReadOnlyList<PetDTO>> ListarAsync(CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.Listar", ActivityKind.Internal);
        var pets = await db.Pets
            .Include(pet => pet.Tutor)
            .ToListAsync(cancellationToken);
        return pets.Select(pet => pet.ToDto()).ToList();
    }

    public async Task<PetDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.BuscarPorId", ActivityKind.Internal);
        var pet = await db.Pets.Include(item => item.Tutor)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return pet?.ToDto();
    }

    public async Task<PetDTO?> CriarAsync(PetDTO dto, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.Criar", ActivityKind.Internal);
        var tutor = await db.Tutores.FindAsync([dto.IdTutor], cancellationToken);
        if (tutor is null) return null;

        var pet = dto.ToEntity();
        db.Pets.Add(pet);
        await db.SaveChangesAsync(cancellationToken);
        pet.Tutor = tutor;
        return pet.ToDto();
    }

    public async Task<bool> AtualizarAsync(int id, PetDTO dto, CancellationToken cancellationToken)
    {
        var pet = await db.Pets.FindAsync([id], cancellationToken);
        if (pet is null) return false;

        pet.UpdateFrom(dto);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken cancellationToken)
    {
        var pet = await db.Pets.FindAsync([id], cancellationToken);
        if (pet is null) return false;

        db.Pets.Remove(pet);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EventoSaudeDTO>?> ListarHistoricoAsync(int idPet, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var eventos = await db.EventosSaude
            .Where(evento => evento.IdPet == idPet)
            .OrderByDescending(evento => evento.DataEvento)
            .ToListAsync(cancellationToken);
        return eventos.Select(evento => evento.ToDto()).ToList();
    }

    public async Task<EventoSaudeDTO?> CriarEventoAsync(int idPet, EventoSaudeDTO dto, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var evento = dto.ToEntity(idPet);
        db.EventosSaude.Add(evento);
        await db.SaveChangesAsync(cancellationToken);
        return evento.ToDto();
    }

    public async Task<EventoSaudeDTO?> BuscarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken)
    {
        var evento = await db.EventosSaude
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        return evento?.ToDto();
    }

    public async Task<bool> AtualizarEventoAsync(int idPet, int idEvento, EventoSaudeDTO dto, CancellationToken cancellationToken)
    {
        var evento = await db.EventosSaude
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        if (evento is null) return false;

        evento.UpdateFrom(dto);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken)
    {
        var evento = await db.EventosSaude
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        if (evento is null) return false;

        db.EventosSaude.Remove(evento);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AlertaPreventivoDTO>?> ListarAlertasAsync(int idPet, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var alertas = await db.AlertasPreventivos
            .Where(alerta => alerta.IdPet == idPet)
            .OrderBy(alerta => alerta.DataPrevista)
            .ToListAsync(cancellationToken);
        return alertas.Select(alerta => alerta.ToDto()).ToList();
    }

    public async Task<AlertaPreventivoDTO?> CriarAlertaAsync(int idPet, AlertaPreventivoDTO dto, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var alerta = dto.ToEntity(idPet);
        db.AlertasPreventivos.Add(alerta);
        await db.SaveChangesAsync(cancellationToken);
        return alerta.ToDto();
    }

    public async Task<AlertaPreventivoDTO?> BuscarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await db.AlertasPreventivos
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        return alerta?.ToDto();
    }

    public async Task<bool> AtualizarAlertaAsync(int idPet, int idAlerta, AlertaPreventivoDTO dto, CancellationToken cancellationToken)
    {
        var alerta = await db.AlertasPreventivos
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        if (alerta is null) return false;

        alerta.UpdateFrom(dto);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await db.AlertasPreventivos
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        if (alerta is null) return false;

        db.AlertasPreventivos.Remove(alerta);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RecomendacaoIaDTO?> GerarRecomendacaoAsync(int idPet, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.GerarRecomendacao", ActivityKind.Internal);
        var pet = await db.Pets
            .Include(item => item.Alertas)
            .Include(item => item.HistoricoSaude)
            .FirstOrDefaultAsync(item => item.Id == idPet, cancellationToken);
        if (pet is null) return null;

        var alertasPendentes = pet.Alertas.Count(alerta =>
            alerta.Status is StatusAlertaComoTexto.Pendente or StatusAlertaComoTexto.Atrasado);
        var ultimoEvento = pet.HistoricoSaude.OrderByDescending(evento => evento.DataEvento).FirstOrDefault();
        var prioridade = pet.ScoreSaude < 60 || alertasPendentes >= 2 ? "Alta" : pet.ScoreSaude < 80 ? "Media" : "Baixa";

        return new RecomendacaoIaDTO
        {
            IdPet = pet.Id,
            NomePet = pet.Nome,
            ScoreSaude = pet.ScoreSaude,
            NivelPrioridade = prioridade,
            Resumo = $"O pet {pet.Nome} possui score {pet.ScoreSaude} e {alertasPendentes} alerta(s) preventivo(s) pendente(s).",
            ProximasAcoes =
            [
                alertasPendentes > 0 ? "Revisar alertas preventivos em aberto." : "Manter rotina preventiva atual.",
                ultimoEvento?.PrecisaRetorno == true ? "Agendar retorno sugerido no ultimo atendimento." : "Registrar novos eventos clinicos quando houver consulta.",
                "A recomendacao da IA e apenas orientativa e nao substitui avaliacao veterinaria."
            ]
        };
    }

    private Task<bool> PetExisteAsync(int idPet, CancellationToken cancellationToken) =>
        db.Pets.AnyAsync(pet => pet.Id == idPet, cancellationToken);
}
