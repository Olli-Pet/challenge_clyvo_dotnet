using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Mappings;
using Olli.Api.Application.Repositories;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;
using Olli.Api.Infrastructure.Observability;
using Olli.Api.Infrastructure.Persistence;

namespace Olli.Api.Application.Services;

public sealed class PetService : IPetService
{
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Tutor> _tutors;
    private readonly IRepository<EventoSaude> _events;
    private readonly IRepository<AlertaPreventivo> _alerts;
    private readonly IRecommendationAuditRepository _auditRepository;
    private readonly ILogger<PetService> _logger;

    public PetService(OlliDb db) : this(
        new Olli.Api.Infrastructure.Persistence.EfRepository<Pet>(db),
        new Olli.Api.Infrastructure.Persistence.EfRepository<Tutor>(db),
        new Olli.Api.Infrastructure.Persistence.EfRepository<EventoSaude>(db),
        new Olli.Api.Infrastructure.Persistence.EfRepository<AlertaPreventivo>(db),
        new Olli.Api.Infrastructure.Persistence.NullRecommendationAuditRepository(),
        NullLogger<PetService>.Instance)
    {
    }

    [ActivatorUtilitiesConstructor]
    public PetService(
        IRepository<Pet> pets,
        IRepository<Tutor> tutors,
        IRepository<EventoSaude> events,
        IRepository<AlertaPreventivo> alerts,
        IRecommendationAuditRepository auditRepository,
        ILogger<PetService> logger)
    {
        _pets = pets;
        _tutors = tutors;
        _events = events;
        _alerts = alerts;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PetDTO>> ListarAsync(CancellationToken cancellationToken, int page = 1, int pageSize = 50, int? idTutor = null, string? especie = null, string? sort = null)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.Listar", ActivityKind.Internal);
        var query = _pets.Query().Include(pet => pet.Tutor).AsQueryable();
        if (idTutor.HasValue)
            query = query.Where(pet => pet.IdTutor == idTutor.Value);
        if (Enum.TryParse<EspeciePetComoTexto>(especie, true, out var species))
            query = query.Where(pet => pet.Especie == species);

        query = sort?.ToLowerInvariant() switch
        {
            "nome" => query.OrderBy(pet => pet.Nome),
            "score" => query.OrderByDescending(pet => pet.ScoreSaude),
            "nascimento" => query.OrderBy(pet => pet.DataNascimento),
            _ => query.OrderBy(pet => pet.Id)
        };
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var pets = await query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(cancellationToken);
        return pets.Select(pet => pet.ToDto()).ToList();
    }

    public async Task<PetDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.BuscarPorId", ActivityKind.Internal);
        var pet = await _pets.Query().Include(item => item.Tutor)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return pet?.ToDto();
    }

    public async Task<PetDTO?> CriarAsync(PetDTO dto, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.Criar", ActivityKind.Internal);
        var tutor = await _tutors.FindAsync([dto.IdTutor], cancellationToken);
        if (tutor is null) return null;

        var pet = dto.ToEntity();
        await _pets.AddAsync(pet, cancellationToken);
        await _pets.SaveChangesAsync(cancellationToken);
        pet.Tutor = tutor;
        return pet.ToDto();
    }

    public async Task<bool> AtualizarAsync(int id, PetDTO dto, CancellationToken cancellationToken)
    {
        var pet = await _pets.FindAsync([id], cancellationToken);
        if (pet is null) return false;

        pet.UpdateFrom(dto);
        await _pets.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken cancellationToken)
    {
        var pet = await _pets.FindAsync([id], cancellationToken);
        if (pet is null) return false;

        _pets.Remove(pet);
        await _pets.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EventoSaudeDTO>?> ListarHistoricoAsync(int idPet, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var eventos = await _events.Query()
            .Where(evento => evento.IdPet == idPet)
            .OrderByDescending(evento => evento.DataEvento)
            .ToListAsync(cancellationToken);
        return eventos.Select(evento => evento.ToDto()).ToList();
    }

    public async Task<EventoSaudeDTO?> CriarEventoAsync(int idPet, EventoSaudeDTO dto, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var evento = dto.ToEntity(idPet);
        await _events.AddAsync(evento, cancellationToken);
        await _events.SaveChangesAsync(cancellationToken);
        return evento.ToDto();
    }

    public async Task<EventoSaudeDTO?> BuscarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken)
    {
        var evento = await _events.Query()
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        return evento?.ToDto();
    }

    public async Task<bool> AtualizarEventoAsync(int idPet, int idEvento, EventoSaudeDTO dto, CancellationToken cancellationToken)
    {
        var evento = await _events.Query()
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        if (evento is null) return false;

        evento.UpdateFrom(dto);
        await _events.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken)
    {
        var evento = await _events.Query()
            .FirstOrDefaultAsync(item => item.Id == idEvento && item.IdPet == idPet, cancellationToken);
        if (evento is null) return false;

        _events.Remove(evento);
        await _events.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AlertaPreventivoDTO>?> ListarAlertasAsync(int idPet, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var alertas = await _alerts.Query()
            .Where(alerta => alerta.IdPet == idPet)
            .OrderBy(alerta => alerta.DataPrevista)
            .ToListAsync(cancellationToken);
        return alertas.Select(alerta => alerta.ToDto()).ToList();
    }

    public async Task<AlertaPreventivoDTO?> CriarAlertaAsync(int idPet, AlertaPreventivoDTO dto, CancellationToken cancellationToken)
    {
        if (!await PetExisteAsync(idPet, cancellationToken)) return null;

        var alerta = dto.ToEntity(idPet);
        await _alerts.AddAsync(alerta, cancellationToken);
        await _alerts.SaveChangesAsync(cancellationToken);
        return alerta.ToDto();
    }

    public async Task<AlertaPreventivoDTO?> BuscarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await _alerts.Query()
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        return alerta?.ToDto();
    }

    public async Task<bool> AtualizarAlertaAsync(int idPet, int idAlerta, AlertaPreventivoDTO dto, CancellationToken cancellationToken)
    {
        var alerta = await _alerts.Query()
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        if (alerta is null) return false;

        alerta.UpdateFrom(dto);
        await _alerts.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await _alerts.Query()
            .FirstOrDefaultAsync(item => item.Id == idAlerta && item.IdPet == idPet, cancellationToken);
        if (alerta is null) return false;

        _alerts.Remove(alerta);
        await _alerts.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RecomendacaoIaDTO?> GerarRecomendacaoAsync(int idPet, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("PetService.GerarRecomendacao", ActivityKind.Internal);
        var pet = await _pets.Query()
            .Include(item => item.Alertas)
            .Include(item => item.HistoricoSaude)
            .FirstOrDefaultAsync(item => item.Id == idPet, cancellationToken);
        if (pet is null) return null;

        var alertasPendentes = pet.Alertas.Count(alerta =>
            alerta.Status is StatusAlertaComoTexto.Pendente or StatusAlertaComoTexto.Atrasado);
        var ultimoEvento = pet.HistoricoSaude.OrderByDescending(evento => evento.DataEvento).FirstOrDefault();
        var prioridade = pet.ScoreSaude < 60 || alertasPendentes >= 2 ? "Alta" : pet.ScoreSaude < 80 ? "Media" : "Baixa";

        var recommendation = new RecomendacaoIaDTO
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
        recommendation.Links = new()
        {
            ["self"] = new($"/pets/{pet.Id}/recomendacao-ia", "GET", "Consultar recomendação"),
            ["pet"] = new($"/pets/{pet.Id}", "GET", "Consultar pet")
        };

        try
        {
            await _auditRepository.AddAsync(
                new RecommendationAudit(pet.Id, pet.ScoreSaude, prioridade, DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Não foi possível registrar auditoria NoSQL da recomendação do pet {PetId}", pet.Id);
        }

        return recommendation;
    }

    private Task<bool> PetExisteAsync(int idPet, CancellationToken cancellationToken) =>
        _pets.Query().AnyAsync(pet => pet.Id == idPet, cancellationToken);
}
