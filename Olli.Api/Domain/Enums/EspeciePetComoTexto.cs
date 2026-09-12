using System.Text.Json.Serialization;

namespace Olli.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EspeciePetComoTexto>))]
public enum EspeciePetComoTexto
{
    Cachorro,
    Gato,
    Passaro,
    Coelho,
    Outro
}
