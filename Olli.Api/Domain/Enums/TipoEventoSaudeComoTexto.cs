using System.Text.Json.Serialization;

namespace Olli.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<TipoEventoSaudeComoTexto>))]
public enum TipoEventoSaudeComoTexto
{
    Vacina,
    Consulta,
    Exame,
    Medicamento,
    Sintoma,
    Checkup
}
