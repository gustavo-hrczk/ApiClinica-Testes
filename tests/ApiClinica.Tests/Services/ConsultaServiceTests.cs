using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Models;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;

namespace ApiClinica.Tests.Services;

public class ConsultaServiceTests : IDisposable
{
    // Horario base sempre no futuro, com segundos zerados para as contas de janela.
    private static readonly DateTime Base = DateTime.Today.AddDays(10).AddHours(10);

    private readonly TestDb _db = new();
    private readonly ConsultaService _service;
    private readonly Paciente _paciente;
    private readonly Medico _medico;

    public ConsultaServiceTests()
    {
        _service = new ConsultaService(_db.Context, new ConsultaMapper());
        _paciente = _db.AddPaciente("529.982.247-25", "Ana");
        _medico = _db.AddMedico("Dr. Carlos");
    }

    public void Dispose() => _db.Dispose();

    private ConsultaCreateDTO Dto(DateTime dataHora, int? pacienteId = null, int? medicoId = null) =>
        new()
        {
            PacienteId = pacienteId ?? _paciente.Id,
            MedicoId = medicoId ?? _medico.Id,
            DataHora = dataHora,
        };

    // ---------- Listar / Buscar ----------

    [Fact]
    public async Task ListarAsync_SemConsultas_RetornaListaVazia()
    {
        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Empty(r.Valor!);
    }

    [Fact]
    public async Task ListarAsync_ComConsultas_RetornaTodas()
    {
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);
        _db.AddConsulta(_paciente.Id, _medico.Id, Base.AddHours(2));

        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Equal(2, r.Valor!.Count);
    }

    [Fact]
    public async Task BuscarPorIdAsync_Existente_RetornaConsulta()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.BuscarPorIdAsync(consulta.Id);

        Assert.True(r.Sucesso);
        Assert.Equal(consulta.Id, r.Valor!.Id);
        Assert.Equal(Base, r.Valor.DataHora);
    }

    [Fact]
    public async Task BuscarPorIdAsync_Inexistente_RetornaNaoEncontrado()
    {
        var r = await _service.BuscarPorIdAsync(9999);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    // ---------- Criar ----------

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteERetornaComId()
    {
        var r = await _service.CriarAsync(Dto(Base));

        Assert.True(r.Sucesso);
        Assert.True(r.Valor!.Id > 0);
        Assert.Equal(_paciente.Id, r.Valor.PacienteId);
        Assert.Equal(_medico.Id, r.Valor.MedicoId);
        Assert.Single(_db.Context.Consultas);
    }

    [Fact]
    public async Task CriarAsync_PacienteInexistente_RetornaInvalido()
    {
        var r = await _service.CriarAsync(Dto(Base, pacienteId: 9999));

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Paciente 9999 não encontrado.", r.Erro);
        Assert.Empty(_db.Context.Consultas);
    }

    [Fact]
    public async Task CriarAsync_MedicoInexistente_RetornaInvalido()
    {
        var r = await _service.CriarAsync(Dto(Base, medicoId: 9999));

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Médico 9999 não encontrado.", r.Erro);
    }

    [Fact]
    public async Task CriarAsync_DataNoPassado_RetornaInvalido()
    {
        var r = await _service.CriarAsync(Dto(DateTime.Now.AddMinutes(-1)));

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Contains("passado", r.Erro);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(29)]
    [InlineData(-15)]
    [InlineData(-29)]
    public async Task CriarAsync_MesmoMedicoDentroDaJanela_RetornaConflito(int minutos)
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.CriarAsync(Dto(Base.AddMinutes(minutos), pacienteId: outroPaciente.Id));

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.StartsWith("Este médico", r.Erro);
        Assert.Single(_db.Context.Consultas);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(-30)]
    [InlineData(60)]
    public async Task CriarAsync_MesmoMedicoNoLimiteDaJanela_EhPermitido(int minutos)
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.CriarAsync(Dto(Base.AddMinutes(minutos), pacienteId: outroPaciente.Id));

        Assert.True(r.Sucesso);
        Assert.Equal(2, _db.Context.Consultas.Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(-29)]
    public async Task CriarAsync_MesmoPacienteDentroDaJanela_RetornaConflito(int minutos)
    {
        var outroMedico = _db.AddMedico("Dra. Bia");
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.CriarAsync(Dto(Base.AddMinutes(minutos), medicoId: outroMedico.Id));

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.StartsWith("Este paciente", r.Erro);
    }

    [Fact]
    public async Task CriarAsync_MesmoPacienteNoLimiteDaJanela_EhPermitido()
    {
        var outroMedico = _db.AddMedico("Dra. Bia");
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.CriarAsync(Dto(Base.AddMinutes(30), medicoId: outroMedico.Id));

        Assert.True(r.Sucesso);
    }

    [Fact]
    public async Task CriarAsync_MedicoEPacienteDiferentesNoMesmoHorario_EhPermitido()
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        var outroMedico = _db.AddMedico("Dra. Bia");
        _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.CriarAsync(Dto(Base, outroPaciente.Id, outroMedico.Id));

        Assert.True(r.Sucesso);
    }

    // ---------- Atualizar ----------

    [Fact]
    public async Task AtualizarAsync_Inexistente_RetornaNaoEncontrado()
    {
        var r = await _service.AtualizarAsync(9999, new ConsultaUpdateDTO { DataHora = Base });

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_ApenasDataHora_MantemPacienteEMedico()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { DataHora = Base.AddHours(4) }
        );

        Assert.True(r.Sucesso);
        var salva = _db.Context.Consultas.Single();
        Assert.Equal(Base.AddHours(4), salva.DataHora);
        Assert.Equal(_paciente.Id, salva.PacienteId);
        Assert.Equal(_medico.Id, salva.MedicoId);
    }

    [Fact]
    public async Task AtualizarAsync_MesmoHorarioQueJaOcupa_NaoConflitaConsigoMesma()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(consulta.Id, new ConsultaUpdateDTO { DataHora = Base });

        Assert.True(r.Sucesso);
    }

    [Fact]
    public async Task AtualizarAsync_NovoMedicoOcupadoNoHorarioAntigo_RetornaConflito()
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        var outroMedico = _db.AddMedico("Dra. Bia");
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);
        _db.AddConsulta(outroPaciente.Id, outroMedico.Id, Base.AddMinutes(10));

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { MedicoId = outroMedico.Id }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Equal(_medico.Id, _db.Context.Consultas.Single(c => c.Id == consulta.Id).MedicoId);
    }

    [Fact]
    public async Task AtualizarAsync_NovoPacienteOcupadoNoHorario_RetornaConflito()
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        var outroMedico = _db.AddMedico("Dra. Bia");
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);
        _db.AddConsulta(outroPaciente.Id, outroMedico.Id, Base);

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { PacienteId = outroPaciente.Id }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.StartsWith("Este paciente", r.Erro);
    }

    [Fact]
    public async Task AtualizarAsync_PacienteInexistente_RetornaInvalido()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { PacienteId = 9999 }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_MedicoInexistente_RetornaInvalido()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(consulta.Id, new ConsultaUpdateDTO { MedicoId = 9999 });

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_DataNoPassado_RetornaInvalidoENaoAltera()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { DataHora = DateTime.Now.AddDays(-1) }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal(Base, _db.Context.Consultas.Single().DataHora);
    }

    [Fact]
    public async Task AtualizarAsync_TrocaPacienteEMedicoValidos_Persiste()
    {
        var outroPaciente = _db.AddPaciente("111.444.777-35", "Bia");
        var outroMedico = _db.AddMedico("Dra. Bia");
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.AtualizarAsync(
            consulta.Id,
            new ConsultaUpdateDTO { PacienteId = outroPaciente.Id, MedicoId = outroMedico.Id }
        );

        Assert.True(r.Sucesso);
        var salva = _db.Context.Consultas.Single();
        Assert.Equal(outroPaciente.Id, salva.PacienteId);
        Assert.Equal(outroMedico.Id, salva.MedicoId);
    }

    // ---------- Excluir ----------

    [Fact]
    public async Task ExcluirAsync_Inexistente_RetornaNaoEncontrado()
    {
        var r = await _service.ExcluirAsync(9999);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public async Task ExcluirAsync_Existente_Remove()
    {
        var consulta = _db.AddConsulta(_paciente.Id, _medico.Id, Base);

        var r = await _service.ExcluirAsync(consulta.Id);

        Assert.True(r.Sucesso);
        Assert.Empty(_db.Context.Consultas);
    }
}
