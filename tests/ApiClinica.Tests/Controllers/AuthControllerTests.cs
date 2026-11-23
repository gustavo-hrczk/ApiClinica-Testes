using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiClinica.Controllers;
using ApiClinica.DTOs;
using ApiClinica.Models;
using ApiClinica.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiClinica.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "chave-de-teste-com-pelo-menos-32-caracteres!!",
                    ["Jwt:Issuer"] = "ApiClinicaTeste",
                    ["Jwt:Audience"] = "ApiClinicaTesteUsuarios",
                }
            )
            .Build();

        _controller = new AuthController(_db.Context, configuracao);
    }

    public void Dispose() => _db.Dispose();

    private void AddUsuario(string login, string senha, string perfil)
    {
        _db.Context.Usuarios.Add(new Usuario { Login = login, Senha = senha, Perfil = perfil });
        _db.Context.SaveChanges();
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_LoginNovo_CriaUsuarioERetorna200()
    {
        var dto = new RegisterDTO { Login = "admin", Senha = "admin123", Perfil = "admin" };

        var resposta = await _controller.Register(dto);

        var ok = Assert.IsType<OkObjectResult>(resposta);
        Assert.Equal("Usuário criado com sucesso", ok.Value.Campo("mensagem"));

        var salvo = Assert.Single(_db.Context.Usuarios);
        Assert.True(salvo.Id > 0);
        Assert.Equal("admin", salvo.Login);
        Assert.Equal("admin123", salvo.Senha);
        Assert.Equal("admin", salvo.Perfil);
    }

    [Fact]
    public async Task Register_SemPerfil_UsaPerfilUsuario()
    {
        var resposta = await _controller.Register(new RegisterDTO { Login = "joao", Senha = "123" });

        Assert.IsType<OkObjectResult>(resposta);
        Assert.Equal("usuario", _db.Context.Usuarios.Single().Perfil);
    }

    [Fact]
    public async Task Register_LoginJaExistente_Retorna400ENaoDuplica()
    {
        AddUsuario("admin", "admin123", "admin");

        var resposta = await _controller.Register(
            new RegisterDTO { Login = "admin", Senha = "outra" }
        );

        var bad = Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal("Usuário já existe", bad.Value.Campo("mensagem"));
        Assert.Single(_db.Context.Usuarios);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_CredenciaisValidas_RetornaTokenComClaimsCorretas()
    {
        AddUsuario("admin", "admin123", "admin");

        var resposta = await _controller.Login(new LoginDTO { Login = "admin", Senha = "admin123" });

        var ok = Assert.IsType<OkObjectResult>(resposta);
        var tokenString = ok.Value.Campo("token");
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        Assert.Equal("ApiClinicaTeste", token.Issuer);
        Assert.Contains("ApiClinicaTesteUsuarios", token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "admin");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
        Assert.True(token.ValidTo > DateTime.UtcNow.AddMinutes(110));
        Assert.True(token.ValidTo <= DateTime.UtcNow.AddMinutes(121));
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401()
    {
        AddUsuario("admin", "admin123", "admin");

        var resposta = await _controller.Login(new LoginDTO { Login = "admin", Senha = "errada" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(resposta);
        Assert.Equal("Usuário ou senha inválidos", unauthorized.Value.Campo("mensagem"));
    }

    [Fact]
    public async Task Login_UsuarioInexistente_Retorna401()
    {
        var resposta = await _controller.Login(new LoginDTO { Login = "ninguem", Senha = "x" });

        Assert.IsType<UnauthorizedObjectResult>(resposta);
    }
}
