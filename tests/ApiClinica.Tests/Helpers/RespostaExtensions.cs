namespace ApiClinica.Tests.Helpers;

public static class RespostaExtensions
{
    /// <summary>
    /// Le uma propriedade de um corpo de resposta anonimo, como new { mensagem = "..." }.
    /// Tipos anonimos sao internos ao assembly da API, entao o acesso e por reflexao.
    /// </summary>
    public static string? Campo(this object? corpo, string nome) =>
        corpo?.GetType().GetProperty(nome)?.GetValue(corpo)?.ToString();
}
