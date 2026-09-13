using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Application.DTOs;

[Serializable]
public class EventoSaudeDTO
{
    [property: Description("Id do evento clinico")]
    public int Id { get; set; }

    [property: Description("Id do pet vinculado ao evento")]
    public int IdPet { get; set; }

    [property: Description("Tipo do evento no historico do pet")]
    public TipoEventoSaudeComoTexto Tipo { get; set; }

    [property: Description("Titulo curto do evento")]
    [property: MinLength(3)]
    [property: MaxLength(160)]
    [property: DefaultValue("Vacina anual")]
    public string? Titulo { get; set; }

    [property: Description("Descricao do que aconteceu ou foi orientado")]
    public string? Descricao { get; set; }

    [property: Description("Data do evento")]
    public DateTime DataEvento { get; set; }

    [property: Description("Nome do veterinario ou clinica")]
    public string? NomeVeterinario { get; set; }

    [property: Description("Indica se o pet precisa de retorno")]
    public bool PrecisaRetorno { get; set; }

    [property: Description("Data sugerida para retorno")]
    public DateTime? DataRetornoSugerida { get; set; }

    public EventoSaudeDTO() { }

    public EventoSaudeDTO(EventoSaude eventoSaude) =>
        (Id, IdPet, Tipo, Titulo, Descricao, DataEvento, NomeVeterinario, PrecisaRetorno, DataRetornoSugerida) =
        (eventoSaude.Id, eventoSaude.IdPet, eventoSaude.Tipo, eventoSaude.Titulo, eventoSaude.Descricao,
            eventoSaude.DataEvento, eventoSaude.NomeVeterinario, eventoSaude.PrecisaRetorno, eventoSaude.DataRetornoSugerida);
}
