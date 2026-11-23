using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;

namespace ApiClinica.Tests.Services;

public class PacienteServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly PacienteService _service;

    public PacienteServiceTests()
    {
        _service = new PacienteService(_db.Context, new PacienteMapper());
    }

    public void Dispose() => _db.Dispose();

    private static PacienteCreateDTO DtoValido() =>
        new()
        {
            Nome = "Ana Souza",
            Email = "ana.souza@example.com",
            Telefone = "(47) 98888-8888",
            DataNasc = new DateOnly(1995, 3, 24),
            Cpf = "529.982.247-25",
        };

    // ---------- Listar / Buscar ----------

    [Fact]
    public async Task ListarAsync_SemPacientes_RetornaListaVazia()
    {
        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Empty(r.Valor!);
    }

    [Fact]
    public async Task ListarAsync_ComPacientes_RetornaTodos()
    {
        _db.AddPaciente("529.982.247-25", "Ana");
        _db.AddPaciente("111.444.777-35", "Bia");

        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Equal(2, r.Valor!.Count);
        Assert.Contains(r.Valor, p => p.Nome == "Ana");
        Assert.Contains(r.Valor, p => p.Nome == "Bia");
    }

    [Fact]
    public async Task BuscarPorIdAsync_Existente_RetornaPaciente()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.BuscarPorIdAsync(paciente.Id);

        Assert.True(r.Sucesso);
        Assert.Equal(paciente.Id, r.Valor!.Id);
        Assert.Equal("Ana Souza", r.Valor.Nome);
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
        var r = await _service.CriarAsync(DtoValido());

        Assert.True(r.Sucesso);
        Assert.True(r.Valor!.Id > 0);
        Assert.Equal("Ana Souza", r.Valor.Nome);

        var salvo = Assert.Single(_db.Context.Pacientes);
        Assert.Equal("529.982.247-25", salvo.Cpf);
    }

    [Fact]
    public async Task CriarAsync_EmailInvalido_RetornaInvalidoENaoPersiste()
    {
        var dto = DtoValido();
        dto.Email = "nao-e-email";

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("E-mail inválido.", r.Erro);
        Assert.Empty(_db.Context.Pacientes);
    }

    [Fact]
    public async Task CriarAsync_TelefoneInvalido_RetornaInvalido()
    {
        var dto = DtoValido();
        dto.Telefone = "123";

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.StartsWith("Telefone inválido.", r.Erro);
    }

    [Fact]
    public async Task CriarAsync_DataNascimentoFutura_RetornaInvalido()
    {
        var dto = DtoValido();
        dto.DataNasc = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Data de nascimento não pode ser futura.", r.Erro);
    }

    [Fact]
    public async Task CriarAsync_DataNascimentoHoje_EhAceita()
    {
        var dto = DtoValido();
        dto.DataNasc = DateOnly.FromDateTime(DateTime.Today);

        var r = await _service.CriarAsync(dto);

        Assert.True(r.Sucesso);
    }

    [Fact]
    public async Task CriarAsync_CpfInvalido_RetornaInvalido()
    {
        var dto = DtoValido();
        dto.Cpf = "111.111.111-11";

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("CPF inválido.", r.Erro);
    }

    [Fact]
    public async Task CriarAsync_CpfDuplicado_RetornaConflito()
    {
        _db.AddPaciente("529.982.247-25");

        var r = await _service.CriarAsync(DtoValido());

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Equal("Já existe um paciente com este CPF.", r.Erro);
        Assert.Single(_db.Context.Pacientes);
    }

    // ---------- Atualizar ----------

    [Fact]
    public async Task AtualizarAsync_Inexistente_RetornaNaoEncontrado()
    {
        var r = await _service.AtualizarAsync(9999, new PacienteUpdateDTO { Nome = "X" });

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_ApenasNome_MantemOsDemaisCampos()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.AtualizarAsync(
            paciente.Id,
            new PacienteUpdateDTO { Nome = "Ana Souza Lima" }
        );

        Assert.True(r.Sucesso);
        Assert.Equal("Ana Souza Lima", r.Valor!.Nome);
        Assert.Equal("ana.souza@example.com", r.Valor.Email);
        Assert.Equal("(47) 98888-8888", r.Valor.Telefone);
        Assert.Equal(new DateOnly(1995, 3, 24), r.Valor.DataNasc);
    }

    [Fact]
    public async Task AtualizarAsync_TodosOsCampos_Persiste()
    {
        var paciente = _db.AddPaciente();
        var dto = new PacienteUpdateDTO
        {
            Nome = "Novo Nome",
            Email = "novo@example.com",
            Telefone = "47 3333-4444",
            DataNasc = new DateOnly(2000, 1, 1),
        };

        var r = await _service.AtualizarAsync(paciente.Id, dto);

        Assert.True(r.Sucesso);
        var salvo = _db.Context.Pacientes.Single();
        Assert.Equal("Novo Nome", salvo.Nome);
        Assert.Equal("novo@example.com", salvo.Email);
        Assert.Equal("47 3333-4444", salvo.Telefone);
        Assert.Equal(new DateOnly(2000, 1, 1), salvo.DataNasc);
        Assert.Equal("529.982.247-25", salvo.Cpf);
    }

    [Fact]
    public async Task AtualizarAsync_EmailInvalido_RetornaInvalidoENaoAltera()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.AtualizarAsync(
            paciente.Id,
            new PacienteUpdateDTO { Nome = "Mudou", Email = "nao-e-email" }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Ana Souza", _db.Context.Pacientes.Single().Nome);
    }

    [Fact]
    public async Task AtualizarAsync_TelefoneInvalido_RetornaInvalido()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.AtualizarAsync(
            paciente.Id,
            new PacienteUpdateDTO { Telefone = "123" }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.StartsWith("Telefone inválido.", r.Erro);
    }

    [Fact]
    public async Task AtualizarAsync_DataNascimentoFutura_RetornaInvalido()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.AtualizarAsync(
            paciente.Id,
            new PacienteUpdateDTO { DataNasc = DateOnly.FromDateTime(DateTime.Today.AddYears(1)) }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Data de Nascimento não pode ser futura.", r.Erro);
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
    public async Task ExcluirAsync_SemConsultas_Remove()
    {
        var paciente = _db.AddPaciente();

        var r = await _service.ExcluirAsync(paciente.Id);

        Assert.True(r.Sucesso);
        Assert.Empty(_db.Context.Pacientes);
    }

    [Fact]
    public async Task ExcluirAsync_ComConsultaFutura_RetornaConflitoENaoRemove()
    {
        var paciente = _db.AddPaciente();
        var medico = _db.AddMedico();
        _db.AddConsulta(paciente.Id, medico.Id, DateTime.Now.AddDays(3));

        var r = await _service.ExcluirAsync(paciente.Id);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Single(_db.Context.Pacientes);
    }

    [Fact]
    public async Task ExcluirAsync_ApenasConsultaPassada_Remove()
    {
        var paciente = _db.AddPaciente();
        var medico = _db.AddMedico();
        _db.AddConsulta(paciente.Id, medico.Id, DateTime.Now.AddDays(-3));

        var r = await _service.ExcluirAsync(paciente.Id);

        Assert.True(r.Sucesso);
        Assert.Empty(_db.Context.Pacientes);
    }

    [Fact]
    public async Task ExcluirAsync_ConsultaFuturaDeOutroPaciente_Remove()
    {
        var paciente = _db.AddPaciente("529.982.247-25");
        var outro = _db.AddPaciente("111.444.777-35");
        var medico = _db.AddMedico();
        _db.AddConsulta(outro.Id, medico.Id, DateTime.Now.AddDays(3));

        var r = await _service.ExcluirAsync(paciente.Id);

        Assert.True(r.Sucesso);
        Assert.Single(_db.Context.Pacientes);
    }
}
