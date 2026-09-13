using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Application.DTOs;

[Serializable]
public class PetDTO
{
    [property: Description("Id do pet")]
    public int Id { get; set; }

    [property: Description("Id do tutor responsavel pelo pet")]
    public int IdTutor { get; set; }

    [property: Description("Nome do tutor responsavel pelo pet")]
    public string? NomeTutor { get; set; }

    [property: Description("Nome do pet")]
    [property: MinLength(2)]
    [property: MaxLength(120)]
    [property: DefaultValue("Olli")]
    public string? Nome { get; set; }

    [property: Description("Especie do pet")]
    public EspeciePetComoTexto Especie { get; set; }

    [property: Description("Raca do pet")]
    [property: MaxLength(120)]
    [property: DefaultValue("Sem raca definida")]
    public string? Raca { get; set; }

    [property: Description("Data de nascimento do pet")]
    public DateTime DataNascimento { get; set; }

    [property: Description("Peso atual em kg")]
    public decimal PesoKg { get; set; }

    [property: Description("Score preventivo de saude calculado pela Olli")]
    public int ScoreSaude { get; set; } = 80;

    [property: Description("Observacoes gerais sobre o pet")]
    public string? Observacoes { get; set; }

    public PetDTO() { }

    public PetDTO(Pet pet) => (Id, IdTutor, NomeTutor, Nome, Especie, Raca, DataNascimento, PesoKg, ScoreSaude, Observacoes) =
        (pet.Id, pet.IdTutor, pet.Tutor?.Nome, pet.Nome, pet.Especie, pet.Raca, pet.DataNascimento, pet.PesoKg, pet.ScoreSaude, pet.Observacoes);
}
