using System.ComponentModel;

namespace Olli.Api.Application.DTOs;

[Serializable]
public class RecomendacaoIaDTO
{
    [property: Description("Id do pet analisado")]
    public int IdPet { get; set; }

    [property: Description("Nome do pet analisado")]
    public string? NomePet { get; set; }

    [property: Description("Score preventivo de saude")]
    public int ScoreSaude { get; set; }

    [property: Description("Nivel de prioridade do acompanhamento")]
    public string? NivelPrioridade { get; set; }

    [property: Description("Resumo da leitura preventiva")]
    public string? Resumo { get; set; }

    [property: Description("Proximas acoes recomendadas para o tutor")]
    public List<string> ProximasAcoes { get; set; } = [];
}
