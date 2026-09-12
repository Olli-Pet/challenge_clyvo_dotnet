using Microsoft.AspNetCore.Mvc;
using Olli.Api.Application.DTOs;
using Olli.Api.Application.Services;
using Olli.Api.Filters;

namespace Olli.Api.Controllers;

[ApiController]
[Route("pets")]
[Produces("application/json")]
public sealed class PetsController(IPetService petService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PetDTO>>> Listar(CancellationToken cancellationToken) =>
        Ok(await petService.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PetDTO>> BuscarPorId(int id, CancellationToken cancellationToken)
    {
        var pet = await petService.BuscarPorIdAsync(id, cancellationToken);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPost]
    [IdempotencyKeyRequired]
    public async Task<ActionResult<PetDTO>> Criar(PetDTO dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest("Nome do pet e obrigatorio.");

        var pet = await petService.CriarAsync(dto, cancellationToken);
        return pet is null ? NotFound() : CreatedAtAction(nameof(BuscarPorId), new { id = pet.Id }, pet);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, PetDTO dto, CancellationToken cancellationToken) =>
        await petService.AtualizarAsync(id, dto, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deletar(int id, CancellationToken cancellationToken) =>
        await petService.DeletarAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<EventoSaudeDTO>>> ListarHistorico(int id, CancellationToken cancellationToken)
    {
        var historico = await petService.ListarHistoricoAsync(id, cancellationToken);
        return historico is null ? NotFound() : Ok(historico);
    }

    [HttpPost("{id:int}/historico")]
    [IdempotencyKeyRequired]
    public async Task<ActionResult<EventoSaudeDTO>> CriarEvento(int id, EventoSaudeDTO dto, CancellationToken cancellationToken)
    {
        var evento = await petService.CriarEventoAsync(id, dto, cancellationToken);
        return evento is null ? NotFound() : CreatedAtAction(nameof(BuscarEvento), new { id, idEvento = evento.Id }, evento);
    }

    [HttpGet("{id:int}/historico/{idEvento:int}")]
    public async Task<ActionResult<EventoSaudeDTO>> BuscarEvento(int id, int idEvento, CancellationToken cancellationToken)
    {
        var evento = await petService.BuscarEventoAsync(id, idEvento, cancellationToken);
        return evento is null ? NotFound() : Ok(evento);
    }

    [HttpPut("{id:int}/historico/{idEvento:int}")]
    public async Task<IActionResult> AtualizarEvento(int id, int idEvento, EventoSaudeDTO dto, CancellationToken cancellationToken) =>
        await petService.AtualizarEventoAsync(id, idEvento, dto, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}/historico/{idEvento:int}")]
    public async Task<IActionResult> DeletarEvento(int id, int idEvento, CancellationToken cancellationToken) =>
        await petService.DeletarEventoAsync(id, idEvento, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/alertas")]
    public async Task<ActionResult<IReadOnlyList<AlertaPreventivoDTO>>> ListarAlertas(int id, CancellationToken cancellationToken)
    {
        var alertas = await petService.ListarAlertasAsync(id, cancellationToken);
        return alertas is null ? NotFound() : Ok(alertas);
    }

    [HttpPost("{id:int}/alertas")]
    [IdempotencyKeyRequired]
    public async Task<ActionResult<AlertaPreventivoDTO>> CriarAlerta(int id, AlertaPreventivoDTO dto, CancellationToken cancellationToken)
    {
        var alerta = await petService.CriarAlertaAsync(id, dto, cancellationToken);
        return alerta is null ? NotFound() : CreatedAtAction(nameof(BuscarAlerta), new { id, idAlerta = alerta.Id }, alerta);
    }

    [HttpGet("{id:int}/alertas/{idAlerta:int}")]
    public async Task<ActionResult<AlertaPreventivoDTO>> BuscarAlerta(int id, int idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await petService.BuscarAlertaAsync(id, idAlerta, cancellationToken);
        return alerta is null ? NotFound() : Ok(alerta);
    }

    [HttpPut("{id:int}/alertas/{idAlerta:int}")]
    public async Task<IActionResult> AtualizarAlerta(int id, int idAlerta, AlertaPreventivoDTO dto, CancellationToken cancellationToken) =>
        await petService.AtualizarAlertaAsync(id, idAlerta, dto, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}/alertas/{idAlerta:int}")]
    public async Task<IActionResult> DeletarAlerta(int id, int idAlerta, CancellationToken cancellationToken) =>
        await petService.DeletarAlertaAsync(id, idAlerta, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/recomendacao-ia")]
    public async Task<ActionResult<RecomendacaoIaDTO>> GerarRecomendacao(int id, CancellationToken cancellationToken)
    {
        var recomendacao = await petService.GerarRecomendacaoAsync(id, cancellationToken);
        return recomendacao is null ? NotFound() : Ok(recomendacao);
    }
}
