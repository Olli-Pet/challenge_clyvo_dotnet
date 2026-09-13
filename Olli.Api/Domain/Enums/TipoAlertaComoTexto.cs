using System.Text.Json.Serialization;

namespace Olli.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<TipoAlertaComoTexto>))]
public enum TipoAlertaComoTexto
{
    Vacina,
    Retorno,
    Checkup,
    Medicamento,
    Exame,
    SinalIoT
}
