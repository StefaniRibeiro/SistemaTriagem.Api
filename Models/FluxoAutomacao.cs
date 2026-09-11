namespace SistemaTriagem.Api.Models;

/// <summary>Registra os eventos de automação disparados via n8n (RF08), permitindo rastreabilidade.</summary>
public class FluxoAutomacao
{
    public int Id { get; set; }
    public int TriagemId { get; set; }
    public Triagem? Triagem { get; set; }

    public TipoFluxoAutomacao Tipo { get; set; }
    public StatusFluxoAutomacao Status { get; set; } = StatusFluxoAutomacao.Pendente;
    public string? Payload { get; set; }   // JSON serializado
    public DateTime? DisparadoEm { get; set; }
}
