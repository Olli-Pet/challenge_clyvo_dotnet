using Microsoft.EntityFrameworkCore;
using Olli.Api.Domain.Entities;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Infrastructure.Persistence;

public static class OlliDbSeed
{
    public static async Task SeedAsync(OlliDb db)
    {
        if (await db.Tutores.AnyAsync())
            return;

        var tutor = new Tutor
        {
            Nome = "Mirelly Santos",
            Email = "mirelly@email.com",
            Telefone = "+55 11 99999-0000"
        };

        var pet = new Pet
        {
            Tutor = tutor,
            Nome = "Olli",
            Especie = EspeciePetComoTexto.Cachorro,
            Raca = "Caramelo",
            DataNascimento = new DateTime(2022, 5, 10),
            PesoKg = 12.4m,
            ScoreSaude = 84,
            Observacoes = "Pet ativo, com acompanhamento preventivo iniciado."
        };

        pet.HistoricoSaude.Add(new EventoSaude
        {
            Tipo = TipoEventoSaudeComoTexto.Vacina,
            Titulo = "Vacinacao anual",
            Descricao = "Vacina anual registrada no historico clinico longitudinal.",
            DataEvento = DateTime.Today.AddMonths(-8),
            NomeVeterinario = "Clinica Clyvo Parceira",
            PrecisaRetorno = true,
            DataRetornoSugerida = DateTime.Today.AddMonths(4)
        });

        pet.Alertas.Add(new AlertaPreventivo
        {
            Tipo = TipoAlertaComoTexto.Checkup,
            Titulo = "Check-up preventivo",
            Descricao = "Acompanhamento recomendado para manter a jornada de saude em dia.",
            DataPrevista = DateTime.Today.AddDays(30),
            Status = StatusAlertaComoTexto.Pendente,
            RecomendacaoIa = "Agendar check-up nos proximos 30 dias e revisar carteira de vacinas."
        });

        db.Tutores.Add(tutor);
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
    }
}
