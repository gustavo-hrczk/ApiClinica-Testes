using ApiClinica.Controllers;
using ApiClinica.DTOs;
using ApiClinica.Interfaces;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApiClinica.Tests.Controllers;

public class MedicosControllerTests
{
    private readonly Mock<IMedicoService> _service = new();
    private readonly MedicosController _controller;

    public MedicosControllerTests()
    {
        _controller = new MedicosController(_service.Object);
    }

    private static MedicoReadDTO Read(int id = 1) =>
        new()
        {
            Id = id,
            Nome = "Dr. Carlos",
            Email = "carlos@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };

    private static MedicoCreateDTO Create() =>
        new()
        {
            Nome = "Dr. Carlos",
            Email = "carlos@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };

    [Fact]
    public async Task GetMedicos_Retorna200ComLista()
    {
        var lista = new List<MedicoReadDTO> { Read(1), Read(2) };
        _service.Setup(s => s.ListarAsync()).ReturnsAsync(Resultado<List<MedicoReadDTO>>.Ok(lista));

        var resposta = await _controller.GetMedicos();

        Assert.Same(lista, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task GetMedicos_FalhaInesperada_Retorna500()
    {
        _service
            .Setup(s => s.ListarAsync())
            .ReturnsAsync(new Resultado<List<MedicoReadDTO>> { Sucesso = false });

        var resposta = await _controller.GetMedicos();

        Assert.Equal(500, Assert.IsType<StatusCodeResult>(resposta).StatusCode);
    }

    [Fact]
    public async Task GetMedicoById_Existente_Retorna200()
    {
        var dto = Read(5);
        _service.Setup(s => s.BuscarPorIdAsync(5)).ReturnsAsync(Resultado<MedicoReadDTO>.Ok(dto));

        var resposta = await _controller.GetMedicoById(5);

        Assert.Same(dto, Assert.IsType<OkObjectResult>(resposta).Value);
    }

    [Fact]
    public async Task GetMedicoById_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.BuscarPorIdAsync(9999))
            .ReturnsAsync(Resultado<MedicoReadDTO>.NaoEncontrado());

        var resposta = await _controller.GetMedicoById(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task CreateMedico_Valido_Retorna201ComLocation()
    {
        var entrada = Create();
        var criado = Read(7);
        _service.Setup(s => s.CriarAsync(entrada)).ReturnsAsync(Resultado<MedicoReadDTO>.Ok(criado));

        var resposta = await _controller.CreateMedico(entrada);

        var created = Assert.IsType<CreatedAtActionResult>(resposta);
        Assert.Equal(nameof(MedicosController.GetMedicoById), created.ActionName);
        Assert.Equal(7, created.RouteValues!["id"]);
        Assert.Same(criado, created.Value);
    }

    [Fact]
    public async Task CreateMedico_Invalido_Retorna400ComMensagem()
    {
        _service
            .Setup(s => s.CriarAsync(It.IsAny<MedicoCreateDTO>()))
            .ReturnsAsync(Resultado<MedicoReadDTO>.Invalido("E-mail inválido."));

        var resposta = await _controller.CreateMedico(Create());

        var bad = Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal("E-mail inválido.", bad.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task UpdateMedico_Valido_Retorna204()
    {
        var entrada = new MedicoUpdateDTO { Telefone = "(47) 96666-6666" };
        _service.Setup(s => s.AtualizarAsync(3, entrada)).ReturnsAsync(Resultado.Ok());

        var resposta = await _controller.UpdateMedico(3, entrada);

        Assert.IsType<NoContentResult>(resposta);
    }

    [Fact]
    public async Task UpdateMedico_Inexistente_Retorna404()
    {
        _service
            .Setup(s => s.AtualizarAsync(9999, It.IsAny<MedicoUpdateDTO>()))
            .ReturnsAsync(Resultado.NaoEncontrado());

        var resposta = await _controller.UpdateMedico(9999, new MedicoUpdateDTO());

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task UpdateMedico_Invalido_Retorna400()
    {
        _service
            .Setup(s => s.AtualizarAsync(3, It.IsAny<MedicoUpdateDTO>()))
            .ReturnsAsync(Resultado.Invalido("Telefone inválido."));

        var resposta = await _controller.UpdateMedico(3, new MedicoUpdateDTO { Telefone = "x" });

        Assert.IsType<BadRequestObjectResult>(resposta);
    }

    [Fact]
    public async Task DeleteMedico_Existente_Retorna204()
    {
        _service.Setup(s => s.ExcluirAsync(3)).ReturnsAsync(Resultado.Ok());

        var resposta = await _controller.DeleteMedico(3);

        Assert.IsType<NoContentResult>(resposta);
        _service.Verify(s => s.ExcluirAsync(3), Times.Once);
    }

    [Fact]
    public async Task DeleteMedico_Inexistente_Retorna404()
    {
        _service.Setup(s => s.ExcluirAsync(9999)).ReturnsAsync(Resultado.NaoEncontrado());

        var resposta = await _controller.DeleteMedico(9999);

        Assert.IsType<NotFoundResult>(resposta);
    }

    [Fact]
    public async Task DeleteMedico_ComConsultaFutura_Retorna409()
    {
        _service.Setup(s => s.ExcluirAsync(3)).ReturnsAsync(Resultado.Conflito("bloqueado"));

        var resposta = await _controller.DeleteMedico(3);

        var conflito = Assert.IsType<ConflictObjectResult>(resposta);
        Assert.Equal("bloqueado", conflito.Value.Campo("mensagem"));
    }
}
