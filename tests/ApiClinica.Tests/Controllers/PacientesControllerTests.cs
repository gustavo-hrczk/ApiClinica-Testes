using ApiClinica.Controllers;
using ApiClinica.DTOs;
using ApiClinica.Interfaces;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApiClinica.Tests.Controllers;

public class PacientesControllerTests
{
    private readonly Mock<IPacienteService> _service = new();
    private readonly PacientesController _controller;

    public PacientesControllerTests()
    {
        _controller = new PacientesController(_service.Object);
    }

    private static PacienteReadDTO Read(int id = 1) =>
        new()
        {
            Id = id,
            Nome = "Ana",
            Email = "ana@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
        };

    private static PacienteCreateDTO Create() =>
        new()
        {
            Nome = "Ana",
            Email = "ana@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
            Cpf = "529.982.247-25",
        };

    [Fact]
    public async Task GetPacientes_Retorna200ComLista()
    {
        var lista = new List<PacienteReadDTO> { Read(1), Read(2) };
        _service.Setup(s => s.ListarAsync()).ReturnsAsync(Resultado<List<PacienteReadDTO>>.Ok(lista));

        var resposta = await _controller.GetPacientes();

        var ok = Assert.IsType<OkObjectResult>(resposta);
        Assert.Same(lista, ok.Value);
    }

    [Fact]
    public async Task GetPacientes_FalhaInesperada_Retorna500()
    {
        _service
            .Setup(s => s.ListarAsync())
            .ReturnsAsync(new Resultado<List<PacienteReadDTO>> { Sucesso = false });

        var resposta = await _controller.GetPacientes();

        Assert.Equal(500, Assert.IsType<StatusCodeResult>(resposta).StatusCode);
    }

    [Fact]
    public async Task GetPacienteById_Existente_Retorna200()
    {
        var dto = Read(5);
        _service.Setup(s => s.BuscarPorIdAsync(5)).ReturnsAsync(Resultado<PacienteReadDTO>.Ok(dto));

        var resposta = await _controller.GetPacienteById(5);

        Assert.Same(dto, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task GetPacienteById_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.BuscarPorIdAsync(9999))
            .ReturnsAsync(Resultado<PacienteReadDTO>.NaoEncontrado());

        var resposta = await _controller.GetPacienteById(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task CreatePaciente_Valido_Retorna201ComLocation()
    {
        var entrada = Create();
        var criado = Read(7);
        _service.Setup(s => s.CriarAsync(entrada)).ReturnsAsync(Resultado<PacienteReadDTO>.Ok(criado));

        var resposta = await _controller.CreatePaciente(entrada);

        var created = Assert.IsType<CreatedAtActionResult>(resposta);
        Assert.Equal(nameof(PacientesController.GetPacienteById), created.ActionName);
        Assert.Equal(7, created.RouteValues!["id"]);
        Assert.Same(criado, created.Value);
    }

    [Fact]
    public async Task CreatePaciente_Invalido_Retorna400ComMensagem()
    {
        _service
            .Setup(s => s.CriarAsync(It.IsAny<PacienteCreateDTO>()))
            .ReturnsAsync(Resultado<PacienteReadDTO>.Invalido("CPF inválido."));

        var resposta = await _controller.CreatePaciente(Create());

        var bad = Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal("CPF inválido.", bad.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task CreatePaciente_CpfDuplicado_Retorna409ComMensagem()
    {
        _service
            .Setup(s => s.CriarAsync(It.IsAny<PacienteCreateDTO>()))
            .ReturnsAsync(Resultado<PacienteReadDTO>.Conflito("Já existe um paciente com este CPF."));

        var resposta = await _controller.CreatePaciente(Create());

        var conflito = Assert.IsType<ConflictObjectResult>(resposta);
        Assert.Equal("Já existe um paciente com este CPF.", conflito.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task PatchPaciente_Valido_Retorna200ComPacienteAtualizado()
    {
        var entrada = new PacienteUpdateDTO { Nome = "Ana Lima" };
        var atualizado = Read(3);
        _service
            .Setup(s => s.AtualizarAsync(3, entrada))
            .ReturnsAsync(Resultado<PacienteReadDTO>.Ok(atualizado));

        var resposta = await _controller.PatchPaciente(3, entrada);

        Assert.Same(atualizado, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task PatchPaciente_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.AtualizarAsync(9999, It.IsAny<PacienteUpdateDTO>()))
            .ReturnsAsync(Resultado<PacienteReadDTO>.NaoEncontrado());

        var resposta = await _controller.PatchPaciente(9999, new PacienteUpdateDTO());

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task PatchPaciente_Invalido_Retorna400()
    {
        _service
            .Setup(s => s.AtualizarAsync(3, It.IsAny<PacienteUpdateDTO>()))
            .ReturnsAsync(Resultado<PacienteReadDTO>.Invalido("E-mail inválido."));

        var resposta = await _controller.PatchPaciente(3, new PacienteUpdateDTO { Email = "x" });

        Assert.IsType<BadRequestObjectResult>(resposta);
    }

    [Fact]
    public async Task DeletePaciente_Existente_Retorna204()
    {
        _service.Setup(s => s.ExcluirAsync(3)).ReturnsAsync(Resultado.Ok());

        var resposta = await _controller.DeletePaciente(3);

        Assert.IsType<NoContentResult>(resposta);
        _service.Verify(s => s.ExcluirAsync(3), Times.Once);
    }

    [Fact]
    public async Task DeletePaciente_Inexistente_Retorna404()
    {
        _service.Setup(s => s.ExcluirAsync(9999)).ReturnsAsync(Resultado.NaoEncontrado());

        var resposta = await _controller.DeletePaciente(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task DeletePaciente_ComConsultaFutura_Retorna409()
    {
        _service.Setup(s => s.ExcluirAsync(3)).ReturnsAsync(Resultado.Conflito("bloqueado"));

        var resposta = await _controller.DeletePaciente(3);

        var conflito = Assert.IsType<ConflictObjectResult>(resposta);
        Assert.Equal("bloqueado", conflito.Value.Campo("mensagem"));
    }
}
