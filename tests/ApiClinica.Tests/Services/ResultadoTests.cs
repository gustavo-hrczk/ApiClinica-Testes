using ApiClinica.Services;

namespace ApiClinica.Tests.Services;

public class ResultadoTests
{
    [Fact]
    public void Ok_SemValor_IndicaSucesso()
    {
        var r = Resultado.Ok();

        Assert.True(r.Sucesso);
        Assert.Equal(TipoErro.Nenhum, r.Tipo);
        Assert.Null(r.Erro);
    }

    [Fact]
    public void NaoEncontrado_SemValor_IndicaFalha()
    {
        var r = Resultado.NaoEncontrado();

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public void Invalido_SemValor_CarregaMensagem()
    {
        var r = Resultado.Invalido("campo ruim");

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("campo ruim", r.Erro);
    }

    [Fact]
    public void Conflito_SemValor_CarregaMensagem()
    {
        var r = Resultado.Conflito("duplicado");

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Equal("duplicado", r.Erro);
    }

    [Fact]
    public void Ok_ComValor_CarregaOValor()
    {
        var r = Resultado<int>.Ok(42);

        Assert.True(r.Sucesso);
        Assert.Equal(42, r.Valor);
        Assert.Equal(TipoErro.Nenhum, r.Tipo);
        Assert.Null(r.Erro);
    }

    [Fact]
    public void NaoEncontrado_ComValor_NaoTrazValor()
    {
        var r = Resultado<string>.NaoEncontrado();

        Assert.False(r.Sucesso);
        Assert.Null(r.Valor);
        Assert.Equal(TipoErro.NaoEncontrado, r.Tipo);
    }

    [Fact]
    public void Invalido_ComValor_CarregaMensagem()
    {
        var r = Resultado<string>.Invalido("campo ruim");

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Invalido, r.Tipo);
        Assert.Equal("campo ruim", r.Erro);
    }

    [Fact]
    public void Conflito_ComValor_CarregaMensagem()
    {
        var r = Resultado<string>.Conflito("duplicado");

        Assert.False(r.Sucesso);
        Assert.Equal(TipoErro.Conflito, r.Tipo);
        Assert.Equal("duplicado", r.Erro);
    }
}
