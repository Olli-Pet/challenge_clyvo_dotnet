using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Olli.Api.Domain.Entities;

namespace Olli.Api.Application.DTOs;

[Serializable]
public class TutorDTO
{
    [property: Description("Id do tutor")]
    public int Id { get; set; }

    [property: Description("Nome completo do tutor")]
    [property: MinLength(3)]
    [property: MaxLength(255)]
    [property: DefaultValue("Maria Silva")]
    public string? Nome { get; set; }

    [property: Description("Email usado para comunicacao preventiva")]
    [property: EmailAddress]
    [property: DefaultValue("maria@email.com")]
    public string? Email { get; set; }

    [property: Description("Telefone ou WhatsApp do tutor")]
    [property: MaxLength(20)]
    [property: DefaultValue("+55 11 99999-0000")]
    public string? Telefone { get; set; }

    [property: Description("Quantidade de pets vinculados ao tutor")]
    public int QuantidadePets { get; set; }

    public TutorDTO() { }

    public TutorDTO(Tutor tutor) => (Id, Nome, Email, Telefone, QuantidadePets) =
        (tutor.Id, tutor.Nome, tutor.Email, tutor.Telefone, tutor.Pets.Count);
}
