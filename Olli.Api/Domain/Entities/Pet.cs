using Olli.Api.Domain.Enums;

namespace Olli.Api.Domain.Entities;

public class Pet
{
    /// <summary>
    /// Id do pet. Este campo e gerado automaticamente pelo DB.
    /// </summary>
    public int Id { get; set; }
    public int IdTutor { get; set; }
    public Tutor? Tutor { get; set; }
    public string? Nome { get; set; }
    public EspeciePetComoTexto Especie { get; set; }
    public string? Raca { get; set; }
    public DateTime DataNascimento { get; set; }
    public decimal PesoKg { get; set; }
    public int ScoreSaude { get; set; } = 80;
    public string? Observacoes { get; set; }
    public List<EventoSaude> HistoricoSaude { get; set; } = [];
    public List<AlertaPreventivo> Alertas { get; set; } = [];
}
