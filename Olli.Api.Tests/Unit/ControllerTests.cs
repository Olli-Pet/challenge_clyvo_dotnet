using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Controllers;
using Olli.Api.Domain.Enums;

namespace Olli.Api.Tests.Unit;

public sealed class ControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CriarPet_ComPetValidoDeveRetornarCreated()
    {
        // Arrange
        var dto = new PetDTO { Id = 10, IdTutor = 1, Nome = "Luna", Especie = EspeciePetComoTexto.Gato };
        var service = new Mock<IPetService>();
        service.Setup(item => item.CriarAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = new PetsController(service.Object);

        // Act
        var result = await controller.Criar(dto, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Same(dto, created.Value);
        service.Verify(item => item.CriarAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CriarTutor_SemNomeDeveRetornarBadRequestESemChamarServico()
    {
        // Arrange
        var service = new Mock<ITutorService>();
        var controller = new TutoresController(service.Object);
        var dto = new TutorDTO { Nome = " " };

        // Act
        var result = await controller.Criar(dto, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        service.Verify(item => item.CriarAsync(It.IsAny<TutorDTO>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CriarPet_ComTutorInexistenteDeveRetornarNotFound()
    {
        // Arrange
        var dto = new PetDTO { IdTutor = 999, Nome = "Luna" };
        var service = new Mock<IPetService>();
        service.Setup(item => item.CriarAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync((PetDTO?)null);
        var controller = new PetsController(service.Object);

        // Act
        var result = await controller.Criar(dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        service.Verify(item => item.CriarAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }
}
