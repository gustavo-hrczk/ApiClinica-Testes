using ApiClinica.DTOs;
using ApiClinica.Mappers;
using ApiClinica.Services;
using ApiClinica.Tests.Helpers;

namespace ApiClinica.Tests.Services;

public class MedicoServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly MedicoService _service;

    public MedicoServiceTests()
    {
        _service = new MedicoService(_db.Context, new MedicoMapper());
    }

    public void Dispose() => _db.Dispose();

    private static MedicoCreateDTO DtoValido() =>
        new()
        {
            Nome = "Dr. Carlos Silva",
            Email = "carlos.silva@example.com",
            Telefone = "(47) 97777-7777",
            CRM = "CRM-SC 12345",
        };

    // ---------- Listar / Buscar ----------

    [Fact]
    public async Task ListarAsync_SemMedicos_RetornaListaVazia()
    {
        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Empty(r.Valor!);
    }

    [Fact]
    public async Task ListarAsync_ComMedicos_RetornaTodos()
    {
        _db.AddMedico("Dr. A");
        _db.AddMedico("Dra. B");

        var r = await _service.ListarAsync();

        Assert.True(r.Sucesso);
        Assert.Equal(2, r.Valor!.Count);
    }

    [Fact]
    public async Task BuscarPorIdAsync_Existente_RetornaMedico()
    {
        var medico = _db.AddMedico();

        var r = await _service.BuscarPorIdAsync(medico.Id);

        Assert.True(r.Sucesso);
        Assert.Equal(medico.Id, r.Valor!.Id);
        Assert.Equal("CRM-SC 12345", r.Valor.CRM);
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
        Assert.Equal("Dr. Carlos Silva", Assert.Single(_db.Context.Medicos).Nome);
    }

    [Fact]
    public async Task CriarAsync_EmailInvalido_RetornaInvalidoENaoPersiste()
    {
        var dto = DtoValido();
        dto.Email = "arroba-faltando";

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("E-mail inválido.", r.Erro);
        Assert.Empty(_db.Context.Medicos);
    }

    [Fact]
    public async Task CriarAsync_TelefoneInvalido_RetornaInvalido()
    {
        var dto = DtoValido();
        dto.Telefone = "abc";

        var r = await _service.CriarAsync(dto);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.StartsWith("Telefone inválido.", r.Erro);
    }

    // ---------- Atualizar ----------

    [Fact]
    public async Task AtualizarAsync_Inexistente_RetornaNaoEncontrado()
    {
        var r = await _service.AtualizarAsync(9999, new MedicoUpdateDTO { Nome = "X" });

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_ApenasTelefone_MantemOsDemaisCampos()
    {
        var medico = _db.AddMedico();

        var r = await _service.AtualizarAsync(
            medico.Id,
            new MedicoUpdateDTO { Telefone = "(47) 96666-6666" }
        );

        Assert.True(r.Sucesso);
        var salvo = _db.Context.Medicos.Single();
        Assert.Equal("(47) 96666-6666", salvo.Telefone);
        Assert.Equal("Dr. Carlos Silva", salvo.Nome);
        Assert.Equal("carlos.silva@example.com", salvo.Email);
        Assert.Equal("CRM-SC 12345", salvo.CRM);
    }

    [Fact]
    public async Task AtualizarAsync_EmailETelefoneValidos_Persiste()
    {
        var medico = _db.AddMedico();

        var r = await _service.AtualizarAsync(
            medico.Id,
            new MedicoUpdateDTO { Email = "novo@example.com", Telefone = "47 3333-4444" }
        );

        Assert.True(r.Sucesso);
        var salvo = _db.Context.Medicos.Single();
        Assert.Equal("novo@example.com", salvo.Email);
        Assert.Equal("47 3333-4444", salvo.Telefone);
    }

    [Fact]
    public async Task AtualizarAsync_EmailInvalido_RetornaInvalidoENaoAltera()
    {
        var medico = _db.AddMedico();

        var r = await _service.AtualizarAsync(
            medico.Id,
            new MedicoUpdateDTO { Nome = "Mudou", Email = "sem-arroba" }
        );

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("Dr. Carlos Silva", _db.Context.Medicos.Single().Nome);
    }

    [Fact]
    public async Task AtualizarAsync_TelefoneInvalido_RetornaInvalido()
    {
        var medico = _db.AddMedico();

        var r = await _service.AtualizarAsync(medico.Id, new MedicoUpdateDTO { Telefone = "xyz" });

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
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
        var medico = _db.AddMedico();

        var r = await _service.ExcluirAsync(medico.Id);

        Assert.True(r.Sucesso);
        Assert.Empty(_db.Context.Medicos);
    }

    [Fact]
    public async Task ExcluirAsync_ComConsultaFutura_RetornaConflitoENaoRemove()
    {
        var paciente = _db.AddPaciente();
        var medico = _db.AddMedico();
        _db.AddConsulta(paciente.Id, medico.Id, DateTime.Now.AddDays(3));

        var r = await _service.ExcluirAsync(medico.Id);

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Single(_db.Context.Medicos);
    }

    [Fact]
    public async Task ExcluirAsync_ApenasConsultaPassada_Remove()
    {
        var paciente = _db.AddPaciente();
        var medico = _db.AddMedico();
        _db.AddConsulta(paciente.Id, medico.Id, DateTime.Now.AddDays(-3));

        var r = await _service.ExcluirAsync(medico.Id);

        Assert.True(r.Sucesso);
        Assert.Empty(_db.Context.Medicos);
    }
}
