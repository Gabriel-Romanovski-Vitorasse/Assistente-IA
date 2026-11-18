using System.Globalization;

namespace AssistenteIA.Models;

/// <summary>
/// Dado "do próprio aplicativo". A IA não conhece esses dados:
/// ela só consegue acessá-los chamando as ferramentas (function calling).
/// </summary>
public sealed record Produto(string Nome, decimal Preco, int Quantidade)
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public string PrecoFormatado => Preco.ToString("C", PtBr);
}
