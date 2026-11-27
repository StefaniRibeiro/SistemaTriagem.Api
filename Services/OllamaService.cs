using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SistemaTriagem.Api.Services;

public interface IOllamaService
{
    /// <summary>
    /// Gera a próxima resposta do chatbot de triagem, combinando o histórico da conversa
    /// com o contexto recuperado via RAG (ChromaDB).
    /// </summary>
    Task<string> GerarRespostaTriagemAsync(IEnumerable<(string origem, string conteudo)> historico, string novaMensagemPaciente);
}

/// <summary>
/// Executa o modelo de linguagem localmente via Ollama (item 2.2 e 2.7 do TCC).
/// Implementa a etapa de "Generation" da arquitetura RAG: recebe o contexto já
/// recuperado do ChromaDB e o injeta no prompt do modelo antes de gerar a resposta,
/// reduzindo o risco de alucinação (RNF04 — nenhum dado é enviado a serviços externos).
/// Quando Ollama:ApiKey está configurada (ambiente cloud sem GPU), usa endpoint
/// compatível com OpenAI/Groq mantendo o mesmo prompt e fluxo RAG.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IChromaDbService _chromaDbService;
    private readonly ILogger<OllamaService> _logger;

    private const string SystemPrompt = """
        Você é um assistente de triagem de pacientes em uma unidade de Atenção Secundária à Saúde.
        Seu papel é APENAS coletar informações de forma clara, empática e objetiva sobre:
        sintomas, intensidade (escala 0 a 10), duração e fatores associados.

        REGRAS OBRIGATÓRIAS:
        - Você NUNCA realiza diagnóstico médico nem sugere condutas terapêuticas.
        - Use linguagem simples, acessível a pessoas com baixa familiaridade tecnológica.
        - Faça uma pergunta objetiva por vez.
        - Responda sempre em texto simples. Nao chame ferramentas, funcoes ou produza saida em JSON.
        - Baseie-se no contexto clínico fornecido (discriminadores do Sistema de Triagem de Manchester)
          apenas para guiar quais perguntas fazer, nunca para informar o paciente sobre gravidade.
        - Nao informe diagnostico nem classifique prioridade ou cor no chat; essa sugestao e calculada pelo sistema e mostrada no painel profissional.
        - O paciente ja esta em uma UPA. Nunca encerre a conversa dizendo para procurar outro servico ou pronto-socorro.
        - Se houver sintomas preocupantes, continue a coleta essencial com perguntas curtas. Nao de orientacoes medicas ou de encaminhamento.
        - Nao encerre com despedidas genericas como "estou aqui para ajudar" ou "estou a disposicao". A triagem so termina quando o sistema registrar a finalizacao.
        """;

    public OllamaService(HttpClient httpClient, IConfiguration config, IChromaDbService chromaDbService, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _chromaDbService = chromaDbService;
        _logger = logger;
    }

    public async Task<string> GerarRespostaTriagemAsync(
        IEnumerable<(string origem, string conteudo)> historico,
        string novaMensagemPaciente)
    {
        // Etapa 1/2 do RAG: recupera trechos relevantes da base de conhecimento (protocolos, sintomas).
        var contexto = await _chromaDbService.BuscarContextoRelevanteAsync(novaMensagemPaciente);
        var blocoContexto = contexto.Count > 0
            ? "Contexto clínico de referência:\n- " + string.Join("\n- ", contexto)
            : "Nenhum contexto adicional recuperado.";

        var mensagens = new List<object>
        {
            new { role = "system", content = SystemPrompt + "\n\n" + blocoContexto }
        };

        foreach (var (origem, conteudo) in historico)
        {
            mensagens.Add(new
            {
                role = origem == "Paciente" ? "user" : "assistant",
                content = conteudo
            });
        }

        mensagens.Add(new { role = "user", content = novaMensagemPaciente });

        var baseUrl = (_config["Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
        var model = _config["Ollama:Model"] ?? "llama3.2";
        var apiKey = _config["Ollama:ApiKey"];

        try
        {
            // Se ApiKey estiver configurada (ex: Groq em produção), usa endpoint OpenAI-compatible;
            // caso contrário, mantém a chamada nativa ao Ollama local.
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return await ChamarApiCompativelAsync(baseUrl, apiKey, model, mensagens);
            }

            var payload = new
            {
                model,
                messages = mensagens,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/chat", payload);
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
            return resultado?.Message?.Content?.Trim()
                   ?? "Desculpe, não consegui processar sua resposta agora. Pode repetir?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao chamar o serviço de IA em {BaseUrl}", baseUrl);
            return "No momento não consigo me comunicar com o assistente de IA. " +
                   "Um atendente poderá te ajudar a continuar a triagem manualmente.";
        }
    }

    private async Task<string> ChamarApiCompativelAsync(string baseUrl, string apiKey, string model, List<object> mensagens)
    {
        var payload = new
        {
            model,
            messages = mensagens,
            include_reasoning = false,
            reasoning_effort = "low",
            tool_choice = "none",
            temperature = 0
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("API de IA retornou HTTP {StatusCode}: {ErrorBody}",
                (int)response.StatusCode, errorBody);
        }
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
        return resultado?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
               ?? "Desculpe, não consegui processar sua resposta agora. Pode repetir?";
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
