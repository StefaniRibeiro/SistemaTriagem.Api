using System.Net.Http.Json;
using System.Text.Json;

namespace SistemaTriagem.Api.Services;

public interface IN8nService
{
    /// <summary>Dispara o webhook do n8n que orquestra notificações e organização de filas (RF08).</summary>
    Task<bool> DispararFluxoFinalizacaoAsync(object payload);
}

/// <summary>
/// Cliente HTTP simples para acionar workflows do n8n via webhook (item 2.4 e 2.6 do TCC).
/// O workflow correspondente está em automation-n8n/workflow_triagem.json.
/// </summary>
public class N8nService : IN8nService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<N8nService> _logger;

    public N8nService(HttpClient httpClient, IConfiguration config, ILogger<N8nService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> DispararFluxoFinalizacaoAsync(object payload)
    {
        var baseUrl = _config["N8n:WebhookBaseUrl"];
        var path = _config["N8n:FinalizacaoTriagemPath"];

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}{path}", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("n8n retornou {StatusCode} ao disparar fluxo de finalização", response.StatusCode);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao disparar webhook do n8n em {BaseUrl}{Path}", baseUrl, path);
            return false;
        }
    }
}
