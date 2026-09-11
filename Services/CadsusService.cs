using SistemaTriagem.Api.DTOs;

namespace SistemaTriagem.Api.Services;

public interface ICadsusService
{
    Task<DadosCadastraisDto?> ConsultarPorCpfOuCnsAsync(string? cpf, string? cns);
}

/// <summary>
/// Integração com a base externa CADSUS/RNDS (RF02).
/// Em MockMode=true, simula respostas para desenvolvimento sem depender do serviço
/// real do governo, que exige certificado/credenciamento institucional.
/// Para produção, substitua a implementação por chamadas reais à API FHIR da RNDS
/// (https://servicos.saude.gov.br/rnds/fhir), configurada em appsettings.json.
/// </summary>
public class CadsusService : ICadsusService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CadsusService> _logger;

    public CadsusService(IConfiguration config, HttpClient httpClient, ILogger<CadsusService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DadosCadastraisDto?> ConsultarPorCpfOuCnsAsync(string? cpf, string? cns)
    {
        var mockMode = _config.GetValue<bool>("Cadsus:MockMode");

        if (mockMode)
        {
            return SimularConsulta(cpf, cns);
        }

        // Integração real (esqueleto): chamada autenticada à API FHIR da RNDS.
        var baseUrl = _config["Cadsus:BaseUrl"];
        var apiKey = _config["Cadsus:ApiKey"];

        try
        {
            var identificador = !string.IsNullOrWhiteSpace(cpf) ? cpf : cns;
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/Patient?identifier={identificador}");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CADSUS/RNDS retornou {StatusCode} para identificador informado", response.StatusCode);
                return null;
            }

            // Aqui seria feito o parse do Bundle FHIR retornado (recurso Patient).
            // Implementação de parsing omitida — depende do formato exato exposto pela RNDS.
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar CADSUS/RNDS");
            return null;
        }
    }

    private static DadosCadastraisDto? SimularConsulta(string? cpf, string? cns)
    {
        if (string.IsNullOrWhiteSpace(cpf) && string.IsNullOrWhiteSpace(cns))
            return null;

        // Simulação determinística para fins de demonstração/desenvolvimento.
        return new DadosCadastraisDto
        {
            Cpf = cpf,
            Cns = cns,
            Nome = "Paciente Simulado",
            DataNascimento = new DateOnly(1980, 1, 1),
            Sexo = "OUTRO",
            Telefone = null
        };
    }
}
