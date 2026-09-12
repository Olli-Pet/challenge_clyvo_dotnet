namespace Olli.Api.Domain.Entities;

public class Tutor
{
    /// <summary>
    /// Id do tutor. Este campo e gerado automaticamente pelo DB.
    /// </summary>
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public List<Pet> Pets { get; set; } = [];
}
