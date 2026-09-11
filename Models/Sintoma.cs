namespace SistemaTriagem.Api.Models;

/// <summary>Sintoma relatado durante a triagem, registrado pelo chatbot (RF04).</summary>
public class Sintoma
{
    public int Id { get; set; }
    public int TriagemId { get; set; }
    public Triagem? Triagem { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public int? Intensidade { get; set; }          // escala 0-10
    public string? Duracao { get; set; }
    public string? DiscriminadorManchester { get; set; }
}
