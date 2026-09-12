using Olli.Api.Application.DTOs;
using Olli.Api.Application.Mappings;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Tests.Unit;

public sealed class DtoMappingsTests
{
    [Theory]
    [InlineData(0, 80)]
    [InlineData(-10, 80)]
    [InlineData(45, 45)]
    [InlineData(140, 100)]
    [Trait("Category", "Unit")]
    public void ToEntity_PetDeveNormalizarScoreDeSaude(int scoreInformado, int scoreEsperado)
    {
        // Arrange
        var dto = new PetDTO
        {
            IdTutor = 1,
            Nome = "Luna",
            Especie = EspeciePetComoTexto.Gato,
            ScoreSaude = scoreInformado
        };

        // Act
        var pet = dto.ToEntity();

        // Assert
        Assert.Equal(scoreEsperado, pet.ScoreSaude);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TutorToDto_DeveInformarQuantidadeDePets()
    {
        // Arrange
        var tutor = new Olli.Api.Domain.Entities.Tutor
        {
            Id = 7,
            Nome = "Ana",
            Pets =
            [
                new() { Nome = "Luna" },
                new() { Nome = "Toby" }
            ]
        };

        // Act
        var dto = tutor.ToDto();

        // Assert
        Assert.Equal(7, dto.Id);
        Assert.Equal("Ana", dto.Nome);
        Assert.Equal(2, dto.QuantidadePets);
    }
}
