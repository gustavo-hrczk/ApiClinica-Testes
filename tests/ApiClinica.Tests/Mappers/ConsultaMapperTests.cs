using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Models;

namespace ApiClinica.Tests.Mappers;

public class ConsultaMapperTests
{
    private static readonly DateTime Data = new(2030, 10, 21, 10, 0, 0);

    private readonly ConsultaMapper _mapper = new();

    [Fact]
    public void ToEntity_CopiaTodosOsCampos()
    {
        var dto = new ConsultaCreateDTO { PacienteId = 1, MedicoId = 2, DataHora = Data };

        var entidade = _mapper.ToEntity(dto);

        Assert.Equal(0, entidade.Id);
        Assert.Equal(1, entidade.PacienteId);
        Assert.Equal(2, entidade.MedicoId);
        Assert.Equal(Data, entidade.DataHora);
    }

    [Fact]
    public void ToReadDTO_CopiaTodosOsCampos()
    {
        var consulta = new Consulta { Id = 9, PacienteId = 1, MedicoId = 2, DataHora = Data };

        var dto = _mapper.ToReadDTO(consulta);

        Assert.Equal(9, dto.Id);
        Assert.Equal(1, dto.PacienteId);
        Assert.Equal(2, dto.MedicoId);
        Assert.Equal(Data, dto.DataHora);
    }

    [Fact]
    public void ApplyUpdate_TodosOsCamposPreenchidos_AtualizaTudo()
    {
        var consulta = new Consulta { Id = 9, PacienteId = 1, MedicoId = 2, DataHora = Data };
        var dto = new ConsultaUpdateDTO { PacienteId = 5, MedicoId = 6, DataHora = Data.AddDays(1) };

        _mapper.ApplyUpdate(dto, consulta);

        Assert.Equal(5, consulta.PacienteId);
        Assert.Equal(6, consulta.MedicoId);
        Assert.Equal(Data.AddDays(1), consulta.DataHora);
    }

    [Fact]
    public void ApplyUpdate_CamposNulos_NaoZeraIds()
    {
        var consulta = new Consulta { Id = 9, PacienteId = 1, MedicoId = 2, DataHora = Data };

        _mapper.ApplyUpdate(new ConsultaUpdateDTO(), consulta);

        Assert.Equal(1, consulta.PacienteId);
        Assert.Equal(2, consulta.MedicoId);
        Assert.Equal(Data, consulta.DataHora);
    }
}
