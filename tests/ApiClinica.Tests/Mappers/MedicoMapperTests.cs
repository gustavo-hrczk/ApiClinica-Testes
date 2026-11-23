using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Models;

namespace ApiClinica.Tests.Mappers;

public class MedicoMapperTests
{
    private readonly MedicoMapper _mapper = new();

    private static Medico NovoMedico() =>
        new()
        {
            Id = 3,
            Nome = "Dr. Carlos",
            Email = "carlos@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };

    [Fact]
    public void ToEntity_CopiaTodosOsCampos()
    {
        var dto = new MedicoCreateDTO
        {
            Nome = "Dr. Carlos",
            Email = "carlos@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };

        var entidade = _mapper.ToEntity(dto);

        Assert.Equal(0, entidade.Id);
        Assert.Equal(dto.Nome, entidade.Nome);
        Assert.Equal(dto.Email, entidade.Email);
        Assert.Equal(dto.Telefone, entidade.Telefone);
        Assert.Equal(dto.CRM, entidade.CRM);
    }

    [Fact]
    public void ToReadDTO_CopiaTodosOsCampos()
    {
        var medico = NovoMedico();

        var dto = _mapper.ToReadDTO(medico);

        Assert.Equal(3, dto.Id);
        Assert.Equal(medico.Nome, dto.Nome);
        Assert.Equal(medico.Email, dto.Email);
        Assert.Equal(medico.Telefone, dto.Telefone);
        Assert.Equal(medico.CRM, dto.CRM);
    }

    [Fact]
    public void ApplyUpdate_TodosOsCamposPreenchidos_AtualizaTudo()
    {
        var medico = NovoMedico();
        var dto = new MedicoUpdateDTO
        {
            Nome = "Dra. Carla",
            Email = "carla@example.com",
            Telefone = "(47) 3333-4444",
            CRM = "CRM-SC 99999",
        };

        _mapper.ApplyUpdate(dto, medico);

        Assert.Equal("Dra. Carla", medico.Nome);
        Assert.Equal("carla@example.com", medico.Email);
        Assert.Equal("(47) 3333-4444", medico.Telefone);
        Assert.Equal("CRM-SC 99999", medico.CRM);
    }

    [Fact]
    public void ApplyUpdate_CamposNulos_NaoSobrescreve()
    {
        var medico = NovoMedico();

        _mapper.ApplyUpdate(new MedicoUpdateDTO(), medico);

        Assert.Equal("Dr. Carlos", medico.Nome);
        Assert.Equal("carlos@example.com", medico.Email);
        Assert.Equal("(47) 97777-7777", medico.Telefone);
        Assert.Equal("CRM-SC 12345", medico.CRM);
    }
}
