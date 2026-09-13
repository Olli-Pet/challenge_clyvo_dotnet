using Microsoft.AspNetCore.Mvc;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Filters;

namespace Olli.Api.Controllers;

[ApiController]
[Route("tutores")]
[Produces("application/json")]
public sealed class TutoresController(ITutorService tutorService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<TutorDTO>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TutorDTO>>> Listar(CancellationToken cancellationToken) =>
        Ok(await tutorService.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<TutorDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TutorDTO>> BuscarPorId(int id, CancellationToken cancellationToken)
    {
        var tutor = await tutorService.BuscarPorIdAsync(id, cancellationToken);
        return tutor is null ? NotFound() : Ok(tutor);
    }

    [HttpPost]
    [IdempotencyKeyRequired]
    [ProducesResponseType<TutorDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TutorDTO>> Criar(TutorDTO dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest("Nome do tutor e obrigatorio.");

        var tutor = await tutorService.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(BuscarPorId), new { id = tutor.Id }, tutor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, TutorDTO dto, CancellationToken cancellationToken) =>
        await tutorService.AtualizarAsync(id, dto, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(int id, CancellationToken cancellationToken) =>
        await tutorService.DeletarAsync(id, cancellationToken) ? NoContent() : NotFound();
}
