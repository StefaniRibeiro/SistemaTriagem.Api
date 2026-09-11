using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Paciente>(e =>
        {
            e.ToTable("paciente");
            e.HasIndex(p => p.Cpf).IsUnique();
            e.HasIndex(p => p.Cns).IsUnique();
        });

        modelBuilder.Entity<Triagem>(e =>
        {
            e.ToTable("triagem");
            e.HasOne(t => t.Paciente)
                .WithMany(p => p.Triagens)
                .HasForeignKey(t => t.PacienteId);

            e.HasOne(t => t.Atendente)
                .WithMany()
                .HasForeignKey(t => t.AtendenteId);

            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Prioridade).HasConversion<string>();
        });

        modelBuilder.Entity<Sintoma>(e =>
        {
            e.ToTable("sintoma");
            e.HasOne(s => s.Triagem)
                .WithMany(t => t.Sintomas)
                .HasForeignKey(s => s.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mensagem>(e =>
        {
            e.ToTable("mensagem");
            e.Property(m => m.Origem).HasConversion<string>();
            e.HasOne(m => m.Triagem)
                .WithMany(t => t.Mensagens)
                .HasForeignKey(m => m.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.HasIndex(u => u.Login).IsUnique();
            e.Property(u => u.Perfil).HasConversion<string>();
        });

        modelBuilder.Entity<FluxoAutomacao>(e =>
        {
            e.ToTable("fluxo_automacao");
            e.Property(f => f.Tipo).HasConversion<string>();
            e.Property(f => f.Status).HasConversion<string>();
            e.HasOne(f => f.Triagem)
                .WithMany(t => t.FluxosAutomacao)
                .HasForeignKey(f => f.TriagemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
