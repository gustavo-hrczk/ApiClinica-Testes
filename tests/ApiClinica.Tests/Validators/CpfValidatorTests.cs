using ApiClinica.Validators;

namespace ApiClinica.Tests.Validators;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    [InlineData("123.456.789-09")]
    public void EhValido_CpfCorreto_RetornaTrue(string cpf)
    {
        Assert.True(CpfValidator.EhValido(cpf));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EhValido_VazioOuNulo_RetornaFalse(string? cpf)
    {
        Assert.False(CpfValidator.EhValido(cpf));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("529.982.247-2")]
    [InlineData("529.982.247-255")]
    [InlineData("abc.def.ghi-jk")]
    public void EhValido_QuantidadeDeDigitosErrada_RetornaFalse(string cpf)
    {
        Assert.False(CpfValidator.EhValido(cpf));
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    [InlineData("99999999999")]
    public void EhValido_TodosOsDigitosIguais_RetornaFalse(string cpf)
    {
        Assert.False(CpfValidator.EhValido(cpf));
    }

    [Theory]
    [InlineData("529.982.247-26")] // segundo digito verificador errado
    [InlineData("529.982.247-35")] // primeiro digito verificador errado
    [InlineData("123.456.789-00")]
    public void EhValido_DigitoVerificadorErrado_RetornaFalse(string cpf)
    {
        Assert.False(CpfValidator.EhValido(cpf));
    }
}
