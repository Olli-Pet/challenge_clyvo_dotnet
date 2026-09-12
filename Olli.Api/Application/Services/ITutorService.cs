using Olli.Api.Application.DTOs;

namespace Olli.Api.Application.Services;

public interface ITutorService
{
    Task<IReadOnlyList<TutorDTO>> ListarAsync(CancellationToken cancellationToken);
    Task<TutorDTO?> BuscarPorIdAsync(int id, CancellationToken cancellationToken);
    Task<TutorDTO> CriarAsync(TutorDTO dto, CancellationToken cancellationToken);
    Task<bool> AtualizarAsync(int id, TutorDTO dto, CancellationToken cancellationToken);
    Task<bool> DeletarAsync(int id, CancellationToken cancellationToken);
}
