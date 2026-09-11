namespace SistemaTriagem.Api.Models;

/// <summary>Representa atendentes e profissionais de saúde cadastrados no sistema.</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool VerificarSenha(string senhaPlana) =>
        BCrypt.Net.BCrypt.Verify(senhaPlana, SenhaHash);
}
