namespace SistemaTriagem.Api.Models;

/// <summary>Histórico de mensagens trocadas durante a triagem (RF06).</summary>
public class Mensagem
{
    public int Id { get; set; }
    public int TriagemId { get; set; }
    public Triagem? Triagem { get; set; }

    public OrigemMensagem Origem { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
