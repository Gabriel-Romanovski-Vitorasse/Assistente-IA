using System.ComponentModel;
using System.Globalization;
using System.Text;
using AssistenteIA.Models;
using Microsoft.Extensions.AI;

namespace AssistenteIA.Ferramentas;

/// <summary>
/// FUNCTION CALLING (ferramentas):
/// métodos C# comuns que a IA pode pedir para executar.
///
/// O atributo [Description] é enviado para o modelo: é assim que ele
/// "entende" para que serve cada método e quais parâmetros passar.
/// Quem executa o método é sempre o nosso app, e nunca a IA diretamente.
/// </summary>
public sealed class FerramentasApp
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Disparado sempre que a IA chama uma ferramenta (usado para mostrar na tela).</summary>
    public event Action<string>? FerramentaExecutada;

    public IReadOnlyList<Produto> Produtos { get; } =
    [
        new("Teclado mecânico", 289.90m, 14),
        new("Mouse sem fio", 119.90m, 32),
        new("Monitor 24 polegadas", 899.00m, 6),
        new("Notebook i5 16GB", 3999.00m, 3),
        new("Headset gamer", 249.90m, 0),
        new("Webcam Full HD", 199.90m, 9)
    ];

    [Description("Retorna a data e a hora atuais do computador do usuário.")]
    public string ObterDataHoraAtual()
    {
        Notificar("ObterDataHoraAtual()");
        return DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy, HH:mm", PtBr);
    }

    [Description("Lista todos os produtos cadastrados no estoque do aplicativo, com preço e quantidade disponível.")]
    public string ListarProdutos()
    {
        Notificar("ListarProdutos()");

        var texto = new StringBuilder();
        foreach (var p in Produtos)
            texto.AppendLine($"- {p.Nome}: {p.PrecoFormatado}, {p.Quantidade} unidade(s) em estoque");

        return texto.ToString();
    }

    [Description("Consulta o preço e a quantidade em estoque de um produto específico pelo nome.")]
    public string ConsultarEstoque(
        [Description("Nome (ou parte do nome) do produto, por exemplo: 'mouse' ou 'monitor'")] string nomeProduto)
    {
        Notificar($"ConsultarEstoque(\"{nomeProduto}\")");

        // Busca sem diferenciar maiúsculas/minúsculas nem acentos.
        var comparador = PtBr.CompareInfo;
        var opcoes = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        var encontrados = Produtos
            .Where(p => comparador.IndexOf(p.Nome, nomeProduto.Trim(), opcoes) >= 0)
            .ToList();

        if (encontrados.Count == 0)
            return $"Nenhum produto encontrado com o nome \"{nomeProduto}\".";

        return string.Join("\n", encontrados.Select(p =>
            $"{p.Nome}: preço {p.PrecoFormatado}, {p.Quantidade} unidade(s) em estoque"));
    }

    /// <summary>
    /// Converte os métodos acima em "ferramentas" que o Microsoft.Extensions.AI
    /// descreve para o modelo (nome, descrição e parâmetros em JSON Schema).
    /// </summary>
    public IList<AITool> ComoFerramentasDeIA() =>
    [
        AIFunctionFactory.Create(ObterDataHoraAtual),
        AIFunctionFactory.Create(ListarProdutos),
        AIFunctionFactory.Create(ConsultarEstoque)
    ];

    private void Notificar(string chamada) => FerramentaExecutada?.Invoke(chamada);
}
