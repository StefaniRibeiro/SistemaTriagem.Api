namespace SistemaTriagem.Api.Models;

/// <summary>
/// Entidade central do modelo (item 4.5.2). Uma triagem pertence a um paciente,
/// é composta por vários sintomas e mensagens, e dispara fluxos de automação ao ser finalizada.
/// </summary>
public class Triagem
{
    public int Id { get; set; }

    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }

    /// <summary>Preenchido quando um atendente conduz a triagem em nome do paciente (RF05).</summary>
    public int? AtendenteId { get; set; }
    public Usuario? Atendente { get; set; }

    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public StatusTriagem Status { get; set; } = StatusTriagem.EmAndamento;
    public PrioridadeTriagem? Prioridade { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? FinalizadaEm { get; set; }

    public ICollection<Sintoma> Sintomas { get; set; } = new List<Sintoma>();
    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    public ICollection<FluxoAutomacao> FluxosAutomacao { get; set; } = new List<FluxoAutomacao>();
}
