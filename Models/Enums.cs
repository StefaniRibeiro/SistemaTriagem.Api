namespace SistemaTriagem.Api.Models;

public enum PerfilUsuario
{
    Atendente,
    ProfissionalSaude,
    Admin
}

public enum StatusTriagem
{
    EmAndamento,
    Finalizada,
    Cancelada
}

/// <summary>
/// Níveis do Sistema de Triagem de Manchester (STM) — MACKWAY-JONES; MARSDEN; WINDLE, 2014.
/// </summary>
public enum PrioridadeTriagem
{
    Emergente = 1,       // vermelho - atendimento imediato
    MuitoUrgente = 2,    // laranja  - até 10 min
    Urgente = 3,         // amarelo  - até 60 min
    PoucoUrgente = 4,    // verde    - até 120 min
    NaoUrgente = 5        // azul     - até 240 min
}

public enum OrigemMensagem
{
    Paciente,
    Chatbot,
    Atendente
}

public enum TipoFluxoAutomacao
{
    NotificacaoFinalizacao,
    NotificacaoPrioridadeAlta,
    Outro
}

public enum StatusFluxoAutomacao
{
    Pendente,
    Disparado,
    Erro
}
