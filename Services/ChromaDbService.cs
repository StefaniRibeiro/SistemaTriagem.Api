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
/// </summary>
public class ChromaDbService : IChromaDbService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ChromaDbService> _logger;

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
                $"{baseUrl}/api/v1/collections/{collection}/query", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ChromaDB retornou {StatusCode} na consulta vetorial", response.StatusCode);
                return new List<string>();
            }

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

            return documentos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar ChromaDB — prosseguindo sem contexto adicional");
            return new List<string>();
        }
    }
}
