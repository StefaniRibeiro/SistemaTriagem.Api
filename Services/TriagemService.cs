using SistemaTriagem.Api.Models;

namespace SistemaTriagem.Api.Services;

public interface ITriagemService
{
    /// <summary>
    /// Sugere uma classificação de prioridade (RF11) com base nos sintomas coletados,
    /// aplicando regras simplificadas derivadas dos discriminadores do Sistema de
    /// Triagem de Manchester (MACKWAY-JONES; MARSDEN; WINDLE, 2014).
    /// IMPORTANTE: é uma sugestão de apoio — a decisão final é sempre do profissional
    /// de saúde (RNF09 — o sistema não realiza diagnóstico médico).
    /// </summary>
    PrioridadeTriagem ClassificarPrioridade(IEnumerable<Sintoma> sintomas);
    PrioridadeTriagem? ClassificarMensagensPaciente(IEnumerable<string> mensagens);
}

public class TriagemService : ITriagemService
{
    // Palavras-chave associadas a discriminadores de alto risco do STM.
    private static readonly string[] DiscriminadoresEmergentes =
    {
        "parada", "inconsciente", "sem resposta", "convulsão", "hemorragia grave",
        "dor torácica intensa", "dor no peito intensa", "falta de ar grave",
        "dificuldade intensa para respirar", "não consigo respirar", "respiração inadequada", "engasgo"
    };

    private static readonly string[] DiscriminadoresMuitoUrgentes =
    {
        "dor torácica", "dor no peito", "falta de ar", "dificuldade para respirar", "febre alta", "confusão mental",
        "sangramento", "dor intensa súbita"
    };

    private static readonly string[] DiscriminadoresUrgentes =
    {
        "febre", "vômito persistente", "dor moderada", "tontura"
    };

    public PrioridadeTriagem ClassificarPrioridade(IEnumerable<Sintoma> sintomas)
    {
        var lista = sintomas.ToList();
        if (lista.Count == 0)
            return PrioridadeTriagem.NaoUrgente;

        var intensidadeMaxima = lista.Max(s => s.Intensidade ?? 0);
        var descricoes = lista.Select(s => (s.Descricao + " " + (s.DiscriminadorManchester ?? "")).ToLowerInvariant());

        bool ContemAlguma(string[] termos) =>
            descricoes.Any(d => termos.Any(t => d.Contains(t, StringComparison.OrdinalIgnoreCase)));

        if (ContemAlguma(DiscriminadoresEmergentes) || intensidadeMaxima >= 10)
            return PrioridadeTriagem.Emergente;

        if (ContemAlguma(DiscriminadoresMuitoUrgentes) || intensidadeMaxima >= 8)
            return PrioridadeTriagem.MuitoUrgente;

        if (ContemAlguma(DiscriminadoresUrgentes) || intensidadeMaxima >= 5)
            return PrioridadeTriagem.Urgente;

        if (intensidadeMaxima >= 2)
            return PrioridadeTriagem.PoucoUrgente;

        return PrioridadeTriagem.NaoUrgente;
    }

    public PrioridadeTriagem? ClassificarMensagensPaciente(IEnumerable<string> mensagens)
    {
        var relato = string.Join(" ", mensagens);
        if (string.IsNullOrWhiteSpace(relato))
            return PrioridadeTriagem.NaoUrgente;

        var intensidade = System.Text.RegularExpressions.Regex.Matches(
                relato,
                @"(?:intensidade|nota|escala|dor)\D{0,20}(10|[0-9])\s*(?:de\s*10)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

        var prioridade = ClassificarPrioridade(new[]
        {
            new Sintoma { Descricao = relato, Intensidade = intensidade > 0 ? intensidade : null }
        });

        // Nao sugere baixa prioridade antes de a conversa ter dados suficientes.
        return prioridade == PrioridadeTriagem.NaoUrgente ? null : prioridade;
    }
}
