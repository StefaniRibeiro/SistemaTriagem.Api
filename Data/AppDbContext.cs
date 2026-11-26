using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaTriagem.Api.Models;

namespace SistemaTriagem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Triagem> Triagens => Set<Triagem>();
    public DbSet<Sintoma> Sintomas => Set<Sintoma>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<FluxoAutomacao> FluxosAutomacao => Set<FluxoAutomacao>();

    private static string ParaUpperSnakeCase(string valor) =>
        Regex.Replace(valor, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant();

    private static ValueConverter<TEnum, string> ConverterEnumSnakeCase<TEnum>()
        where TEnum : struct, Enum =>
        new(
            v => ParaUpperSnakeCase(v.ToString()),
            v => Enum.Parse<TEnum>(v.Replace("_", string.Empty), true)
        );

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Paciente>(e =>
        {
            e.ToTable("paciente");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Cpf).HasColumnName("cpf");
            e.Property(p => p.Cns).HasColumnName("cns");
            e.Property(p => p.Nome).HasColumnName("nome");
            e.Property(p => p.DataNascimento).HasColumnName("data_nascimento");
            e.Property(p => p.Sexo).HasColumnName("sexo");
            e.Property(p => p.Telefone).HasColumnName("telefone");
            e.Property(p => p.CriadoEm).HasColumnName("criado_em");
            e.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
            e.Ignore(p => p.Idade);
            e.HasIndex(p => p.Cpf).IsUnique();
            e.HasIndex(p => p.Cns).IsUnique();
        });

        modelBuilder.Entity<Triagem>(e =>
        {
            e.ToTable("triagem");
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.PacienteId).HasColumnName("paciente_id");
            e.Property(t => t.AtendenteId).HasColumnName("atendente_id");
            e.Property(t => t.DataHora).HasColumnName("data_hora");
            e.Property(t => t.Status).HasColumnName("status").HasConversion(ConverterEnumSnakeCase<StatusTriagem>());
            e.Property(t => t.Prioridade).HasColumnName("prioridade").HasConversion(ConverterEnumSnakeCase<PrioridadeTriagem>());
            e.Property(t => t.Observacoes).HasColumnName("observacoes");
            e.Property(t => t.FinalizadaEm).HasColumnName("finalizada_em");

            e.HasOne(t => t.Paciente)
                .WithMany(p => p.Triagens)
                .HasForeignKey(t => t.PacienteId);

            e.HasOne(t => t.Atendente)
                .WithMany()
                .HasForeignKey(t => t.AtendenteId);
        });

        modelBuilder.Entity<Sintoma>(e =>
        {
            e.ToTable("sintoma");
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.TriagemId).HasColumnName("triagem_id");
            e.Property(s => s.Descricao).HasColumnName("descricao");
            e.Property(s => s.Intensidade).HasColumnName("intensidade");
            e.Property(s => s.Duracao).HasColumnName("duracao");
            e.Property(s => s.DiscriminadorManchester).HasColumnName("discriminador_manchester");

            e.HasOne(s => s.Triagem)
                .WithMany(t => t.Sintomas)
                .HasForeignKey(s => s.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mensagem>(e =>
        {
            e.ToTable("mensagem");
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.TriagemId).HasColumnName("triagem_id");
            e.Property(m => m.Origem).HasColumnName("origem").HasConversion(ConverterEnumSnakeCase<OrigemMensagem>());
            e.Property(m => m.Conteudo).HasColumnName("conteudo");
            e.Property(m => m.Timestamp).HasColumnName("timestamp");

            e.HasOne(m => m.Triagem)
                .WithMany(t => t.Mensagens)
                .HasForeignKey(m => m.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Nome).HasColumnName("nome");
            e.Property(u => u.Login).HasColumnName("login");
            e.Property(u => u.SenhaHash).HasColumnName("senha_hash");
            e.Property(u => u.Perfil).HasColumnName("perfil").HasConversion(ConverterEnumSnakeCase<PerfilUsuario>());
            e.Property(u => u.Ativo).HasColumnName("ativo");
            e.Property(u => u.CriadoEm).HasColumnName("criado_em");
            e.HasIndex(u => u.Login).IsUnique();
        });

        modelBuilder.Entity<FluxoAutomacao>(e =>
        {
            e.ToTable("fluxo_automacao");
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.TriagemId).HasColumnName("triagem_id");
            e.Property(f => f.Tipo).HasColumnName("tipo").HasConversion(ConverterEnumSnakeCase<TipoFluxoAutomacao>());
            e.Property(f => f.Status).HasColumnName("status").HasConversion(ConverterEnumSnakeCase<StatusFluxoAutomacao>());
            e.Property(f => f.Payload).HasColumnName("payload");
            e.Property(f => f.DisparadoEm).HasColumnName("disparado_em");

            e.HasOne(f => f.Triagem)
                .WithMany(t => t.FluxosAutomacao)
                .HasForeignKey(f => f.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
