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
}

public class TriagemService : ITriagemService
{
    // Palavras-chave associadas a discriminadores de alto risco do STM.
    private static readonly string[] DiscriminadoresEmergentes =
    {
        "parada", "inconsciente", "sem resposta", "convulsão", "hemorragia grave",
        "dor torácica intensa", "falta de ar grave", "engasgo"
    };

    private static readonly string[] DiscriminadoresMuitoUrgentes =
    {
        "dor torácica", "falta de ar", "febre alta", "confusão mental",
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
}
