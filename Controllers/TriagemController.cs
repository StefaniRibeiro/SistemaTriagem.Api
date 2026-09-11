using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTriagem.Api.Data;
using SistemaTriagem.Api.DTOs;
using SistemaTriagem.Api.Models;
using SistemaTriagem.Api.Services;
using System.Text.Json;

namespace SistemaTriagem.Api.Controllers;

/// <summary>Módulo de Triagem e Automação (item 4.2 do TCC) — RF07, RF08, RF11.</summary>
[ApiController]
[Route("api/[controller]")]
public class TriagemController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITriagemService _triagemService;
    private readonly IN8nService _n8nService;

    public TriagemController(AppDbContext db, ITriagemService triagemService, IN8nService n8nService)
    {
        _db = db;
        _triagemService = triagemService;
        _n8nService = n8nService;
    }

    /// <summary>
    /// RF07: registra sintomas e finaliza a triagem no banco de dados.
    /// RF11: sugere a classificação de prioridade (Sistema de Triagem de Manchester).
    /// RF08: dispara o fluxo de notificação via n8n.
    /// </summary>
    [HttpPost("finalizar")]
    public async Task<ActionResult<FinalizarTriagemResponse>> Finalizar([FromBody] FinalizarTriagemRequest request)
    {
        var triagem = await _db.Triagens
            .Include(t => t.Paciente)
            .Include(t => t.Sintomas)
            .FirstOrDefaultAsync(t => t.Id == request.TriagemId);

        if (triagem is null)
            return NotFound("Triagem não encontrada.");

        if (triagem.Status == StatusTriagem.Finalizada)
            return BadRequest("Esta triagem já está finalizada.");

        foreach (var s in request.Sintomas)
        {
            triagem.Sintomas.Add(new Sintoma
            {
                TriagemId = triagem.Id,
                Descricao = s.Descricao,
                Intensidade = s.Intensidade,
                Duracao = s.Duracao,
                DiscriminadorManchester = s.DiscriminadorManchester
            });
        }

        var prioridade = _triagemService.ClassificarPrioridade(triagem.Sintomas);

        triagem.Prioridade = prioridade;
        triagem.Status = StatusTriagem.Finalizada;
        triagem.Observacoes = request.Observacoes;
        triagem.FinalizadaEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // RF08: dispara notificação via n8n (webhook -> automation-n8n/workflow_triagem.json)
        var payload = new
        {
            triagemId = triagem.Id,
            paciente = triagem.Paciente?.Nome,
            prioridade = prioridade.ToString(),
            dataHora = triagem.FinalizadaEm
        };

        var disparado = await _n8nService.DispararFluxoFinalizacaoAsync(payload);

        _db.FluxosAutomacao.Add(new FluxoAutomacao
        {
            TriagemId = triagem.Id,
            Tipo = prioridade <= PrioridadeTriagem.MuitoUrgente
                ? TipoFluxoAutomacao.NotificacaoPrioridadeAlta
                : TipoFluxoAutomacao.NotificacaoFinalizacao,
            Status = disparado ? StatusFluxoAutomacao.Disparado : StatusFluxoAutomacao.Erro,
            Payload = JsonSerializer.Serialize(payload),
            DisparadoEm = disparado ? DateTime.UtcNow : null
        });
        await _db.SaveChangesAsync();

        return Ok(new FinalizarTriagemResponse(triagem.Id, prioridade.ToString(), disparado));
    }
}
