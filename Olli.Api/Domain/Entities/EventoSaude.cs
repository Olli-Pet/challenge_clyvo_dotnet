using Olli.Api.Domain.Enums;

namespace Olli.Api.Domain.Entities;

public class EventoSaude
{
    /// <summary>
    /// Id do evento clinico. Este campo e gerado automaticamente pelo DB.
    /// </summary>
    public int Id { get; set; }
    public int IdPet { get; set; }
    public Pet? Pet { get; set; }
    public TipoEventoSaudeComoTexto Tipo { get; set; }
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public DateTime DataEvento { get; set; }
    public string? NomeVeterinario { get; set; }
    public bool PrecisaRetorno { get; set; }
    public DateTime? DataRetornoSugerida { get; set; }
}
