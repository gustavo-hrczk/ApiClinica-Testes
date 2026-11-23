using ApiClinica.Validators;

namespace ApiClinica.Tests.Validators;

public class ContatoValidatorTests
{
    [Theory]
    [InlineData("ana.souza@example.com")]
    [InlineData("a@b.co")]
    public void ValidarEmail_Valido_RetornaNull(string email)
    {
        Assert.Null(ContatoValidator.ValidarEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-email")]
    [InlineData("@example.com")]
    [InlineData("ana@")]
    public void ValidarEmail_Invalido_RetornaMensagem(string? email)
    {
        Assert.Equal("E-mail inválido.", ContatoValidator.ValidarEmail(email));
    }

    [Theory]
    [InlineData("(47) 98888-8888")]
    [InlineData("(47)988888888")]
    [InlineData("47988888888")]
    [InlineData("47 3333-4444")]
    [InlineData("  (47) 98888-8888  ")]
    public void ValidarTelefone_Valido_RetornaNull(string telefone)
    {
        Assert.Null(ContatoValidator.ValidarTelefone(telefone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("(47) 9888-888")]
    [InlineData("(047) 98888-8888")]
    [InlineData("47 988888-8888")]
    public void ValidarTelefone_Invalido_RetornaMensagem(string? telefone)
    {
        var erro = ContatoValidator.ValidarTelefone(telefone);

        Assert.NotNull(erro);
        Assert.StartsWith("Telefone inválido.", erro);
    }
}
