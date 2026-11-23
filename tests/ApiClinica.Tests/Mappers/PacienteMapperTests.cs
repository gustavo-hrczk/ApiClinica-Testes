using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Models;

namespace ApiClinica.Tests.Mappers;

public class PacienteMapperTests
{
    private readonly PacienteMapper _mapper = new();

    private static Paciente NovoPaciente() =>
        new()
        {
            Id = 7,
            Nome = "Ana Souza",
            Email = "ana@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
            Cpf = "529.982.247-25",
        };

    [Fact]
    public void ToEntity_CopiaTodosOsCampos()
    {
        var dto = new PacienteCreateDTO
        {
            Nome = "Ana Souza",
            Email = "ana@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
            Cpf = "529.982.247-25",
        };

        var entidade = _mapper.ToEntity(dto);

        Assert.Equal(0, entidade.Id);
        Assert.Equal(dto.Nome, entidade.Nome);
        Assert.Equal(dto.Email, entidade.Email);
        Assert.Equal(dto.Telefone, entidade.Telefone);
        Assert.Equal(dto.DataNasc, entidade.DataNasc);
        Assert.Equal(dto.Cpf, entidade.Cpf);
    }

    [Fact]
    public void ToReadDTO_CopiaCamposPublicos_ENaoExpoeCpf()
    {
        var paciente = NovoPaciente();

        var dto = _mapper.ToReadDTO(paciente);

        Assert.Equal(7, dto.Id);
        Assert.Equal(paciente.Nome, dto.Nome);
        Assert.Equal(paciente.Email, dto.Email);
        Assert.Equal(paciente.Telefone, dto.Telefone);
        Assert.Equal(paciente.DataNasc, dto.DataNasc);
        Assert.Null(typeof(PacienteReadDTO).GetProperty("Cpf"));
    }

    [Fact]
    public void ApplyUpdate_TodosOsCamposPreenchidos_AtualizaTudoMenosCpf()
    {
        var paciente = NovoPaciente();
        var dto = new PacienteUpdateDTO
        {
            Nome = "Ana Lima",
            Email = "ana.lima@example.com",
            Telefone = "(47) 3333-4444",
            DataNasc = new DateOnly(1990, 1, 1),
        };

        _mapper.ApplyUpdate(dto, paciente);

        Assert.Equal("Ana Lima", paciente.Nome);
        Assert.Equal("ana.lima@example.com", paciente.Email);
        Assert.Equal("(47) 3333-4444", paciente.Telefone);
        Assert.Equal(new DateOnly(1990, 1, 1), paciente.DataNasc);
        Assert.Equal("529.982.247-25", paciente.Cpf);
    }

    [Fact]
    public void ApplyUpdate_CamposNulos_NaoSobrescreve()
    {
        var paciente = NovoPaciente();

        _mapper.ApplyUpdate(new PacienteUpdateDTO(), paciente);

        Assert.Equal("Ana Souza", paciente.Nome);
        Assert.Equal("ana@example.com", paciente.Email);
        Assert.Equal("(47) 98888-8888", paciente.Telefone);
        Assert.Equal(new DateOnly(1995, 3, 24), paciente.DataNasc);
    }
}
