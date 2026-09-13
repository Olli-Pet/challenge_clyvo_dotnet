using Olli.Api.Application.DTOs;

namespace Olli.Api.Application.Services;

public interface IPetService
{
    Task<IReadOnlyList<PetDTO>> ListarAsync(CancellationToken cancellationToken);
    Task<PetDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken);
    Task<PetDTO?> CriarAsync(PetDTO dto, CancellationToken cancellationToken);
    Task<bool> AtualizarAsync(int id, PetDTO dto, CancellationToken cancellationToken);
    Task<bool> DeletarAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EventoSaudeDTO>?> ListarHistoricoAsync(int idPet, CancellationToken cancellationToken);
    Task<EventoSaudeDTO?> CriarEventoAsync(int idPet, EventoSaudeDTO dto, CancellationToken cancellationToken);
    Task<EventoSaudeDTO?> BuscarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken);
    Task<bool> AtualizarEventoAsync(int idPet, int idEvento, EventoSaudeDTO dto, CancellationToken cancellationToken);
    Task<bool> DeletarEventoAsync(int idPet, int idEvento, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertaPreventivoDTO>?> ListarAlertasAsync(int idPet, CancellationToken cancellationToken);
    Task<AlertaPreventivoDTO?> CriarAlertaAsync(int idPet, AlertaPreventivoDTO dto, CancellationToken cancellationToken);
    Task<AlertaPreventivoDTO?> BuscarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken);
    Task<bool> AtualizarAlertaAsync(int idPet, int idAlerta, AlertaPreventivoDTO dto, CancellationToken cancellationToken);
    Task<bool> DeletarAlertaAsync(int idPet, int idAlerta, CancellationToken cancellationToken);
    Task<RecomendacaoIaDTO?> GerarRecomendacaoAsync(int idPet, CancellationToken cancellationToken);
}
