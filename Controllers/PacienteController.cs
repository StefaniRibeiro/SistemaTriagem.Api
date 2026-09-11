using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTriagem.Api.Data;
using SistemaTriagem.Api.DTOs;
using SistemaTriagem.Api.Models;
using SistemaTriagem.Api.Services;

namespace SistemaTriagem.Api.Controllers;

/// <summary>Módulo de Identificação do Paciente (item 4.2 do TCC) — RF01, RF02, RF03.</summary>
[ApiController]
[Route("api/[controller]")]
public class PacienteController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICadsusService _cadsusService;

    public PacienteController(AppDbContext db, ICadsusService cadsusService)
    {
        _db = db;
        _cadsusService = cadsusService;
    }

    /// <summary>
    /// RF01: recebe CPF ou CNS. RF02: consulta CADSUS/RNDS. RF03: já cria a triagem
    /// em andamento para que o chatbot inicie a interação automaticamente.
    /// </summary>
    [HttpPost("identificar")]
    public async Task<ActionResult<IdentificarPacienteResponse>> Identificar([FromBody] IdentificarPacienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Cpf) && string.IsNullOrWhiteSpace(request.Cns))
            return BadRequest("Informe o CPF ou o Cartão Nacional de Saúde (CNS).");

        var dados = await _cadsusService.ConsultarPorCpfOuCnsAsync(request.Cpf, request.Cns);
        if (dados is null)
            return NotFound("Não foi possível localizar os dados cadastrais do paciente.");

        // Reaproveita o cadastro se o paciente já tiver sido identificado anteriormente.
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p =>
            (request.Cpf != null && p.Cpf == request.Cpf) ||
            (request.Cns != null && p.Cns == request.Cns));

        if (paciente is null)
        {
            paciente = new Paciente
            {
                Cpf = dados.Cpf,
                Cns = dados.Cns,
                Nome = dados.Nome,
                DataNascimento = dados.DataNascimento,
                Sexo = dados.Sexo,
                Telefone = dados.Telefone
            };
            _db.Pacientes.Add(paciente);
            await _db.SaveChangesAsync();
        }

        var triagem = new Triagem
        {
            PacienteId = paciente.Id,
            Status = StatusTriagem.EmAndamento,
            DataHora = DateTime.UtcNow
        };
        _db.Triagens.Add(triagem);
        await _db.SaveChangesAsync();

        // Primeira mensagem de boas-vindas do chatbot já é registrada (RF06).
        _db.Mensagens.Add(new Mensagem
        {
            TriagemId = triagem.Id,
            Origem = OrigemMensagem.Chatbot,
            Conteudo = $"Olá, {paciente.Nome.Split(' ')[0]}! Vou fazer algumas perguntas rápidas " +
                       "para entender melhor como você está se sentindo hoje. Pode ficar à vontade!"
        });
        await _db.SaveChangesAsync();

        return Ok(new IdentificarPacienteResponse(paciente.Id, triagem.Id, dados));
    }
}
