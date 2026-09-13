using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Application.DTOs;

[Serializable]
public class AlertaPreventivoDTO
{
    [property: Description("Id do alerta preventivo")]
    public int Id { get; set; }

    [property: Description("Id do pet vinculado ao alerta")]
    public int IdPet { get; set; }

    [property: Description("Tipo do alerta")]
    public TipoAlertaComoTexto Tipo { get; set; }

    [property: Description("Titulo curto do alerta")]
    [property: MinLength(3)]
    [property: MaxLength(160)]
    [property: DefaultValue("Reforco de vacina")]
    public string? Titulo { get; set; }

    [property: Description("Descricao do cuidado preventivo")]
    public string? Descricao { get; set; }

    [property: Description("Data prevista para o cuidado")]
    public DateTime DataPrevista { get; set; }

    [property: Description("Status atual do alerta")]
    public StatusAlertaComoTexto Status { get; set; } = StatusAlertaComoTexto.Pendente;

    [property: Description("Recomendacao gerada pela IA da Olli")]
    public string? RecomendacaoIa { get; set; }

    public AlertaPreventivoDTO() { }

    public AlertaPreventivoDTO(AlertaPreventivo alerta) =>
        (Id, IdPet, Tipo, Titulo, Descricao, DataPrevista, Status, RecomendacaoIa) =
        (alerta.Id, alerta.IdPet, alerta.Tipo, alerta.Titulo, alerta.Descricao, alerta.DataPrevista, alerta.Status, alerta.RecomendacaoIa);
}
