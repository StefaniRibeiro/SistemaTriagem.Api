using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTriagem.Api.Data;
using SistemaTriagem.Api.DTOs;
using SistemaTriagem.Api.Models;
using SistemaTriagem.Api.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaTriagem.Api.Controllers;

/// <summary>Módulo de Chatbot (item 4.2 do TCC) — RF04, RF05, RF06.</summary>
[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOllamaService _ollamaService;
    private readonly ITriagemService _triagemService;

    public ChatbotController(AppDbContext db, IOllamaService ollamaService, ITriagemService triagemService)
    {
        _db = db;
        _ollamaService = ollamaService;
        _triagemService = triagemService;
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

        // Atualiza a prioridade preliminar para que a equipe veja sinais de risco
        // no painel durante a conversa.
        var relatosPaciente = await _db.Mensagens
            .Where(m => m.TriagemId == triagem.Id && m.Origem == OrigemMensagem.Paciente)
            .OrderBy(m => m.Timestamp)
            .Select(m => m.Conteudo)
            .ToListAsync();
        var prioridadeSugerida = _triagemService.ClassificarMensagensPaciente(relatosPaciente);
        if (prioridadeSugerida is not null &&
            (triagem.Prioridade is null || prioridadeSugerida.Value < triagem.Prioridade.Value))
            triagem.Prioridade = prioridadeSugerida.Value;
        await _db.SaveChangesAsync();

        var historico = triagem.Mensagens
            .OrderBy(m => m.Timestamp)
            .Select(m => (m.Origem.ToString(), m.Conteudo));

        var respostaTexto = await _ollamaService.GerarRespostaTriagemAsync(historico, request.Conteudo);
        if (ContemEncaminhamentoParaOutroServico(respostaTexto))
        {
            respostaTexto = "Entendi. Vou registrar o que você relatou para a equipe da unidade. " +
                "O que mais você está sentindo?";
        }

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
    private static bool ContemEncaminhamentoParaOutroServico(string resposta)
    {
        var semAcentos = string.Concat(resposta
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
        var texto = semAcentos.ToLowerInvariant().Replace('\u2011', '-');

        return texto.Contains("pronto-socorro") || texto.Contains("servico de emergencia") ||
            Regex.IsMatch(texto,
                @"\b(procure|busque|va|dirija-se|ligue|acione)\b.{0,100}\b(unidade|hospital|pronto|emergencia|ajuda|profissional)\b");
    }

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
