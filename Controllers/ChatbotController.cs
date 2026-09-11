using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTriagem.Api.Data;
using SistemaTriagem.Api.DTOs;
using SistemaTriagem.Api.Models;
using SistemaTriagem.Api.Services;

namespace SistemaTriagem.Api.Controllers;

/// <summary>Módulo de Chatbot (item 4.2 do TCC) — RF04, RF05, RF06.</summary>
[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOllamaService _ollamaService;

    public ChatbotController(AppDbContext db, IOllamaService ollamaService)
    {
        _db = db;
        _ollamaService = ollamaService;
    }

    /// <summary>
    /// Recebe a mensagem do paciente (ou do atendente operando em seu nome — RF05),
    /// gera a resposta via Ollama+RAG e registra ambas as mensagens (RF06).
    /// </summary>
    [HttpPost("mensagem")]
    public async Task<ActionResult<EnviarMensagemResponse>> EnviarMensagem([FromBody] EnviarMensagemRequest request)
    {
        var triagem = await _db.Triagens
            .Include(t => t.Mensagens)
            .FirstOrDefaultAsync(t => t.Id == request.TriagemId);

        if (triagem is null)
            return NotFound("Triagem não encontrada.");

        if (triagem.Status != StatusTriagem.EmAndamento)
            return BadRequest("Esta triagem já foi finalizada ou cancelada.");

        if (string.IsNullOrWhiteSpace(request.Conteudo))
            return BadRequest("A mensagem não pode estar vazia.");

        var mensagemPaciente = new Mensagem
        {
            TriagemId = triagem.Id,
            Origem = OrigemMensagem.Paciente,
            Conteudo = request.Conteudo
        };
        _db.Mensagens.Add(mensagemPaciente);
        await _db.SaveChangesAsync();

        var historico = triagem.Mensagens
            .OrderBy(m => m.Timestamp)
            .Select(m => (m.Origem.ToString(), m.Conteudo));

        var respostaTexto = await _ollamaService.GerarRespostaTriagemAsync(historico, request.Conteudo);

        var mensagemChatbot = new Mensagem
        {
            TriagemId = triagem.Id,
            Origem = OrigemMensagem.Chatbot,
            Conteudo = respostaTexto
        };
        _db.Mensagens.Add(mensagemChatbot);
        await _db.SaveChangesAsync();

        return Ok(new EnviarMensagemResponse(respostaTexto, mensagemPaciente.Id, mensagemChatbot.Id));
    }

    /// <summary>Retorna o histórico de mensagens de uma triagem (usado para renderizar a Tela 2).</summary>
    [HttpGet("{triagemId:int}/historico")]
    public async Task<ActionResult<List<MensagemDto>>> ObterHistorico(int triagemId)
    {
        var mensagens = await _db.Mensagens
            .Where(m => m.TriagemId == triagemId)
            .OrderBy(m => m.Timestamp)
            .Select(m => new MensagemDto(m.Origem.ToString(), m.Conteudo, m.Timestamp))
            .ToListAsync();

        return Ok(mensagens);
    }
}
