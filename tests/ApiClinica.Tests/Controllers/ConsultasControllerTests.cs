using ApiClinica.Controllers;
using ApiClinica.DTOs;
using ApiClinica.Interfaces;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApiClinica.Tests.Controllers;

public class ConsultasControllerTests
{
    private static readonly DateTime Data = new(2030, 10, 21, 10, 0, 0);

    private readonly Mock<IConsultaService> _service = new();
    private readonly ConsultasController _controller;

    public ConsultasControllerTests()
    {
        _controller = new ConsultasController(_service.Object);
    }

    private static ConsultaReadDTO Read(int id = 1) =>
        new() { Id = id, PacienteId = 1, MedicoId = 2, DataHora = Data };

    private static ConsultaCreateDTO Create() =>
        new() { PacienteId = 1, MedicoId = 2, DataHora = Data };

    [Fact]
    public async Task GetConsultas_Retorna200ComLista()
    {
        var lista = new List<ConsultaReadDTO> { Read(1), Read(2) };
        _service.Setup(s => s.ListarAsync()).ReturnsAsync(Resultado<List<ConsultaReadDTO>>.Ok(lista));

        var resposta = await _controller.GetConsultas();

        Assert.Same(lista, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task GetConsultas_FalhaInesperada_Retorna500()
    {
        _service
            .Setup(s => s.ListarAsync())
            .ReturnsAsync(new Resultado<List<ConsultaReadDTO>> { Sucesso = false });

        var resposta = await _controller.GetConsultas();

        Assert.Equal(500, Assert.IsType<StatusCodeResult>(resposta).StatusCode);
    }

    [Fact]
    public async Task GetConsultaById_Existente_Retorna200()
    {
        var dto = Read(5);
        _service.Setup(s => s.BuscarPorIdAsync(5)).ReturnsAsync(Resultado<ConsultaReadDTO>.Ok(dto));

        var resposta = await _controller.GetConsultaById(5);

        Assert.Same(dto, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task GetConsultaById_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.BuscarPorIdAsync(9999))
            .ReturnsAsync(Resultado<ConsultaReadDTO>.NaoEncontrado());

        var resposta = await _controller.GetConsultaById(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task CreateConsulta_Valida_Retorna201ComLocation()
    {
        var entrada = Create();
        var criada = Read(7);
        _service.Setup(s => s.CriarAsync(entrada)).ReturnsAsync(Resultado<ConsultaReadDTO>.Ok(criada));

        var resposta = await _controller.CreateConsulta(entrada);

        var created = Assert.IsType<CreatedAtActionResult>(resposta);
        Assert.Equal(nameof(ConsultasController.GetConsultaById), created.ActionName);
        Assert.Equal(7, created.RouteValues!["id"]);
        Assert.Same(criada, created.Value);
    }

    [Fact]
    public async Task CreateConsulta_PacienteInexistente_Retorna400ComMensagem()
    {
        _service
            .Setup(s => s.CriarAsync(It.IsAny<ConsultaCreateDTO>()))
            .ReturnsAsync(Resultado<ConsultaReadDTO>.Invalido("Paciente 9999 não encontrado."));

        var resposta = await _controller.CreateConsulta(Create());

        var bad = Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal("Paciente 9999 não encontrado.", bad.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task CreateConsulta_HorarioOcupado_Retorna409ComMensagem()
    {
        _service
            .Setup(s => s.CriarAsync(It.IsAny<ConsultaCreateDTO>()))
            .ReturnsAsync(Resultado<ConsultaReadDTO>.Conflito("horário conflitante"));

        var resposta = await _controller.CreateConsulta(Create());

        var conflito = Assert.IsType<ConflictObjectResult>(resposta);
        Assert.Equal("horário conflitante", conflito.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task UpdateConsulta_Valida_Retorna204()
    {
        var entrada = new ConsultaUpdateDTO { DataHora = Data.AddHours(4) };
        _service.Setup(s => s.AtualizarAsync(3, entrada)).ReturnsAsync(Resultado.Ok());

        var resposta = await _controller.UpdateConsulta(3, entrada);

        Assert.IsType<NoContentResult>(resposta);
    }

    [Fact]
    public async Task UpdateConsulta_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.AtualizarAsync(9999, It.IsAny<ConsultaUpdateDTO>()))
            .ReturnsAsync(Resultado.NaoEncontrado());

        var resposta = await _controller.UpdateConsulta(9999, new ConsultaUpdateDTO());

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task UpdateConsulta_Conflito_Retorna409()
    {
        _service
            .Setup(s => s.AtualizarAsync(3, It.IsAny<ConsultaUpdateDTO>()))
            .ReturnsAsync(Resultado.Conflito("horário conflitante"));

        var resposta = await _controller.UpdateConsulta(3, new ConsultaUpdateDTO { MedicoId = 2 });

        Assert.IsType<ConflictObjectResult>(resposta);
    }

    [Fact]
    public async Task UpdateConsulta_Invalida_Retorna400()
    {
        _service
            .Setup(s => s.AtualizarAsync(3, It.IsAny<ConsultaUpdateDTO>()))
            .ReturnsAsync(Resultado.Invalido("no passado"));

        var resposta = await _controller.UpdateConsulta(3, new ConsultaUpdateDTO());

        Assert.IsType<BadRequestObjectResult>(resposta);
    }

    [Fact]
    public async Task DeleteConsulta_Existente_Retorna204()
    {
        _service.Setup(s => s.ExcluirAsync(3)).ReturnsAsync(Resultado.Ok());

        var resposta = await _controller.DeleteConsulta(3);

        Assert.IsType<NoContentResult>(resposta);
        _service.Verify(s => s.ExcluirAsync(3), Times.Once);
    }

    [Fact]
    public async Task DeleteConsulta_Inexistente_Retorna404()
    {
        _service.Setup(s => s.ExcluirAsync(9999)).ReturnsAsync(Resultado.NaoEncontrado());

        var resposta = await _controller.DeleteConsulta(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }
}
