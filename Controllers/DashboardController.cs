using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTriagem.Api.Data;
using SistemaTriagem.Api.DTOs;
using SistemaTriagem.Api.Models;

namespace SistemaTriagem.Api.Controllers;

/// <summary>Módulo de Painel Administrativo (item 4.2 do TCC) — RF09, RF10. Restrito a profissionais de saúde.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ProfissionalSaude,Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<DashboardResumoDto>> Resumo()
    {
        var hoje = DateTime.UtcNow.Date;

        var triagensHoje = _db.Triagens.Where(t => t.DataHora >= hoje);

        var resumo = new DashboardResumoDto
        {
            TriagensHoje = await triagensHoje.CountAsync(),
            Aguardando = await triagensHoje.CountAsync(t => t.Status == StatusTriagem.EmAndamento),
            Urgentes = await triagensHoje.CountAsync(t =>
                t.Prioridade == PrioridadeTriagem.Emergente || t.Prioridade == PrioridadeTriagem.MuitoUrgente),
            Finalizados = await triagensHoje.CountAsync(t => t.Status == StatusTriagem.Finalizada)
        };

        return Ok(resumo);
    }

    /// <summary>RF09: lista de pacientes triados, com filtro opcional por prioridade.</summary>
    [HttpGet("triagens")]
    public async Task<ActionResult<List<TriagemResumoDto>>> ListarTriagens([FromQuery] string? prioridade)
    {
        var query = _db.Triagens
            .Include(t => t.Paciente)
            .Include(t => t.Sintomas)
            .OrderByDescending(t => t.DataHora)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(prioridade) &&
            Enum.TryParse<PrioridadeTriagem>(prioridade, true, out var p))
        {
            query = query.Where(t => t.Prioridade == p);
        }

        var resultado = await query.Select(t => new TriagemResumoDto
        {
            Id = t.Id,
            PacienteNome = t.Paciente!.Nome,
            PacienteIdade = t.Paciente.Idade,
            DataHora = t.DataHora,
            Prioridade = t.Prioridade != null ? t.Prioridade.ToString() : null,
            Status = t.Status.ToString(),
            PrincipalSintoma = t.Sintomas.Select(s => s.Descricao).FirstOrDefault()
        }).ToListAsync();

        return Ok(resultado);
    }

    /// <summary>RF10: detalhes completos de uma triagem específica.</summary>
    [HttpGet("triagens/{id:int}")]
    public async Task<ActionResult<TriagemDetalheDto>> ObterDetalhes(int id)
    {
        var triagem = await _db.Triagens
            .Include(t => t.Paciente)
            .Include(t => t.Sintomas)
            .Include(t => t.Mensagens)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (triagem is null)
            return NotFound();

        var dto = new TriagemDetalheDto
        {
            Id = triagem.Id,
            PacienteNome = triagem.Paciente!.Nome,
            PacienteIdade = triagem.Paciente.Idade,
            DataHora = triagem.DataHora,
            Prioridade = triagem.Prioridade?.ToString(),
            Status = triagem.Status.ToString(),
            Observacoes = triagem.Observacoes,
            PrincipalSintoma = triagem.Sintomas.Select(s => s.Descricao).FirstOrDefault(),
            Sintomas = triagem.Sintomas
                .Select(s => new SintomaInputDto(s.Descricao, s.Intensidade, s.Duracao, s.DiscriminadorManchester))
                .ToList(),
            Mensagens = triagem.Mensagens
                .OrderBy(m => m.Timestamp)
                .Select(m => new MensagemDto(m.Origem.ToString(), m.Conteudo, m.Timestamp))
                .ToList()
        };

        return Ok(dto);
    }
}
