using System.Net.Http.Json;
using System.Text.Json;

namespace SistemaTriagem.Api.Services;

public interface IChromaDbService
{
    /// <summary>Recupera os trechos da base de conhecimento mais relevantes semanticamente para a consulta.</summary>
    Task<List<string>> BuscarContextoRelevanteAsync(string consulta, int topK = 3);
}

/// <summary>
/// Cliente para o ChromaDB (banco vetorial), usado na etapa de "Retrieval" da arquitetura RAG
/// (item 2.7 do TCC). Pressupõe que a coleção de conhecimento (sintomas, protocolos de triagem,
/// discriminadores do STM) já foi populada pelo script em ai-rag/ingest_chromadb.py.
/// Quando o servidor ChromaDB não está acessível (ex.: ambiente cloud sem container vetorial),
/// utiliza fallback local sobre os arquivos Markdown em KnowledgeBase/.
/// </summary>
public class ChromaDbService : IChromaDbService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ChromaDbService> _logger;

    private static readonly Lazy<List<string>> _secoesLocais = new(CarregarSecoesLocais);

    public ChromaDbService(HttpClient httpClient, IConfiguration config, ILogger<ChromaDbService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<List<string>> BuscarContextoRelevanteAsync(string consulta, int topK = 3)
    {
        var baseUrl = _config["ChromaDb:BaseUrl"];
        var collection = _config["ChromaDb:CollectionName"];

        if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(collection))
        {
            try
            {
                // ChromaDB expõe uma API HTTP simples (v1/v2 conforme a versão instalada).
                // O endpoint abaixo assume o servidor `chroma run` padrão, com a coleção
                // já criada pelo script de ingestão.
                var payload = new
                {
                    query_texts = new[] { consulta },
                    n_results = topK
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{baseUrl.TrimEnd('/')}/api/v1/collections/{collection}/query", payload);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    var documentos = new List<string>();
                    if (doc.RootElement.TryGetProperty("documents", out var documentsArray) &&
                        documentsArray.GetArrayLength() > 0)
                    {
                        foreach (var d in documentsArray[0].EnumerateArray())
                        {
                            var texto = d.GetString();
                            if (!string.IsNullOrWhiteSpace(texto))
                                documentos.Add(texto);
                        }
                    }

                    if (documentos.Count > 0)
                        return documentos;
                }
                else
                {
                    _logger.LogWarning("ChromaDB retornou {StatusCode} na consulta vetorial; usando base local.", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB indisponível em {BaseUrl}; usando base de conhecimento local.", baseUrl);
            }
        }

        return BuscarNaBaseLocal(consulta, topK);
    }

    private static List<string> BuscarNaBaseLocal(string consulta, int topK)
    {
        var secoes = _secoesLocais.Value;
        if (secoes.Count == 0)
            return new List<string>();

        var termos = consulta
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ';', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3)
            .Distinct()
            .ToArray();

        if (termos.Length == 0)
            return secoes.Take(topK).ToList();

        var ranqueados = secoes
            .Select(s => new
            {
                Secao = s,
                Pontos = termos.Count(t => s.Contains(t, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Pontos)
            .Take(topK)
            .Select(x => x.Secao)
            .ToList();

        return ranqueados;
    }

    private static List<string> CarregarSecoesLocais()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
        if (!Directory.Exists(dir))
            return new List<string>();

        var secoes = new List<string>();
        foreach (var arquivo in Directory.GetFiles(dir, "*.md"))
        {
            var conteudo = File.ReadAllText(arquivo);
            var blocos = conteudo.Split("\n## ", StringSplitOptions.RemoveEmptyEntries);
            foreach (var bloco in blocos)
            {
                var limpo = bloco.Trim();
                if (!string.IsNullOrWhiteSpace(limpo))
                    secoes.Add(limpo.StartsWith('#') ? limpo : "## " + limpo);
            }
        }

        return secoes;
    }
}
