namespace SistemaTriagem.Api.DTOs;

// ---------- Identificação do paciente (RF01, RF02) ----------
public record IdentificarPacienteRequest(string? Cpf, string? Cns);

public class DadosCadastraisDto
{
    public string? Cpf { get; set; }
    public string? Cns { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly? DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public string? Telefone { get; set; }
}

public record IdentificarPacienteResponse(int PacienteId, int TriagemId, DadosCadastraisDto Dados);

// ---------- Chatbot (RF03, RF04, RF06) ----------
public record EnviarMensagemRequest(int TriagemId, string Conteudo);
public record EnviarMensagemResponse(string RespostaChatbot, int MensagemPacienteId, int MensagemChatbotId);

// ---------- Finalização da triagem (RF07, RF08, RF11) ----------
public record SintomaInputDto(string Descricao, int? Intensidade, string? Duracao, string? DiscriminadorManchester);
public record FinalizarTriagemRequest(int TriagemId, List<SintomaInputDto> Sintomas, string? Observacoes);
public record FinalizarTriagemResponse(int TriagemId, string PrioridadeSugerida, bool FluxoAutomacaoDisparado);

// ---------- Painel administrativo (RF09, RF10) ----------
public class TriagemResumoDto
{
    public int Id { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public int? PacienteIdade { get; set; }
    public DateTime DataHora { get; set; }
    public string? Prioridade { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PrincipalSintoma { get; set; }
}

public class TriagemDetalheDto : TriagemResumoDto
{
    public List<SintomaInputDto> Sintomas { get; set; } = new();
    public List<MensagemDto> Mensagens { get; set; } = new();
    public string? Observacoes { get; set; }
}

public record MensagemDto(string Origem, string Conteudo, DateTime Timestamp);

public class DashboardResumoDto
{
    public int TriagensHoje { get; set; }
    public int Aguardando { get; set; }
    public int Urgentes { get; set; }
    public int Finalizados { get; set; }
}

// ---------- Autenticação ----------
public record LoginRequest(string Login, string Senha);
public record LoginResponse(string Token, string Nome, string Perfil);
