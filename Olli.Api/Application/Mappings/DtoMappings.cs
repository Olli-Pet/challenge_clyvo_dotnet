using Olli.Api.Application.DTOs;
using Olli.Api.Domain.Entities;

namespace Olli.Api.Application.Mappings;

public static class DtoMappings
{
    public static TutorDTO ToDto(this Tutor tutor) => new(tutor);

    public static PetDTO ToDto(this Pet pet) => new(pet);

    public static EventoSaudeDTO ToDto(this EventoSaude evento) => new(evento);

    public static AlertaPreventivoDTO ToDto(this AlertaPreventivo alerta) => new(alerta);

    public static Tutor ToEntity(this TutorDTO dto) => new()
    {
        Nome = dto.Nome,
        Email = dto.Email,
        Telefone = dto.Telefone
    };

    public static Pet ToEntity(this PetDTO dto) => new()
    {
        IdTutor = dto.IdTutor,
        Nome = dto.Nome,
        Especie = dto.Especie,
        Raca = dto.Raca,
        DataNascimento = dto.DataNascimento,
        PesoKg = dto.PesoKg,
        ScoreSaude = dto.ScoreSaude <= 0 ? 80 : Math.Clamp(dto.ScoreSaude, 0, 100),
        Observacoes = dto.Observacoes
    };

    public static EventoSaude ToEntity(this EventoSaudeDTO dto, int idPet) => new()
    {
        IdPet = idPet,
        Tipo = dto.Tipo,
        Titulo = dto.Titulo,
        Descricao = dto.Descricao,
        DataEvento = dto.DataEvento,
        NomeVeterinario = dto.NomeVeterinario,
        PrecisaRetorno = dto.PrecisaRetorno,
        DataRetornoSugerida = dto.DataRetornoSugerida
    };

    public static AlertaPreventivo ToEntity(this AlertaPreventivoDTO dto, int idPet) => new()
    {
        IdPet = idPet,
        Tipo = dto.Tipo,
        Titulo = dto.Titulo,
        Descricao = dto.Descricao,
        DataPrevista = dto.DataPrevista,
        Status = dto.Status,
        RecomendacaoIa = dto.RecomendacaoIa
    };

    public static void UpdateFrom(this Tutor tutor, TutorDTO dto) =>
        (tutor.Nome, tutor.Email, tutor.Telefone) = (dto.Nome, dto.Email, dto.Telefone);

    public static void UpdateFrom(this Pet pet, PetDTO dto) =>
        (pet.Nome, pet.Especie, pet.Raca, pet.DataNascimento, pet.PesoKg, pet.ScoreSaude, pet.Observacoes) =
        (dto.Nome, dto.Especie, dto.Raca, dto.DataNascimento, dto.PesoKg, Math.Clamp(dto.ScoreSaude, 0, 100), dto.Observacoes);

    public static void UpdateFrom(this EventoSaude evento, EventoSaudeDTO dto) =>
        (evento.Tipo, evento.Titulo, evento.Descricao, evento.DataEvento, evento.NomeVeterinario, evento.PrecisaRetorno, evento.DataRetornoSugerida) =
        (dto.Tipo, dto.Titulo, dto.Descricao, dto.DataEvento, dto.NomeVeterinario, dto.PrecisaRetorno, dto.DataRetornoSugerida);

    public static void UpdateFrom(this AlertaPreventivo alerta, AlertaPreventivoDTO dto) =>
        (alerta.Tipo, alerta.Titulo, alerta.Descricao, alerta.DataPrevista, alerta.Status, alerta.RecomendacaoIa) =
        (dto.Tipo, dto.Titulo, dto.Descricao, dto.DataPrevista, dto.Status, dto.RecomendacaoIa);
}
