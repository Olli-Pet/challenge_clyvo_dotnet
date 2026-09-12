using Olli.Api.Domain.Enums;

namespace Olli.Api.Domain.Entities;

public class AlertaPreventivo
{
    /// <summary>
    /// Id do alerta preventivo. Este campo e gerado automaticamente pelo DB.
    /// </summary>
    public int Id { get; set; }
    public int IdPet { get; set; }
    public Pet? Pet { get; set; }
    public TipoAlertaComoTexto Tipo { get; set; }
    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public DateTime DataPrevista { get; set; }
    public StatusAlertaComoTexto Status { get; set; } = StatusAlertaComoTexto.Pendente;
    public string? RecomendacaoIa { get; set; }
}
