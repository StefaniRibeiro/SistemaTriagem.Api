namespace SistemaTriagem.Api.Models;

/// <summary>
/// Armazena os dados de identificação recuperados da base CADSUS/RNDS (RF01, RF02).
/// </summary>
public class Paciente
{
    public int Id { get; set; }
    public string? Cpf { get; set; }
    public string? Cns { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly? DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public string? Telefone { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public ICollection<Triagem> Triagens { get; set; } = new List<Triagem>();

    public int? Idade =>
        DataNascimento is null
            ? null
            : DateTime.Today.Year - DataNascimento.Value.Year -
              (DateTime.Today.DayOfYear < DataNascimento.Value.DayOfYear ? 1 : 0);
}
