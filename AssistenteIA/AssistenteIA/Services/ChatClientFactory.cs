using System.ClientModel;
using AssistenteIA.Models;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace AssistenteIA.Services;

/// <summary>
/// Padrão Factory: centraliza a criação do cliente de IA.
///
/// PONTO-CHAVE DO SEMINÁRIO: todos os provedores devolvem um IChatClient
/// (interface do Microsoft.Extensions.AI). O restante do app só conhece
/// a interface, então trocar de provedor não muda nenhuma outra linha
/// do sistema (polimorfismo + baixo acoplamento).
/// </summary>
public static class ChatClientFactory
{
    public const string UrlOllama = "http://localhost:11434";

    /// <param name="chaveDigitada">Chave informada na tela. Se vazia, usa a variável de ambiente.</param>
    public static IChatClient Criar(OpcaoProvedor provedor, string modelo, string? chaveDigitada, bool usarFerramentas)
    {
        IChatClient clienteBase = provedor.Tipo == ProvedorIA.Ollama
            ? new OllamaApiClient(new Uri(UrlOllama), modelo)
            : CriarClienteCompativelComOpenAI(provedor, modelo, chaveDigitada);

        // ChatClientBuilder funciona como um "pipeline" (padrão Decorator):
        // cada Use...() embrulha o cliente e adiciona um comportamento.
        var construtor = new ChatClientBuilder(clienteBase);

        if (usarFerramentas)
        {
            // Quando a IA pede para executar uma ferramenta, este middleware
            // chama o método C# correspondente e devolve o resultado para a IA.
            construtor.UseFunctionInvocation();
        }

        return construtor.Build();
    }

    /// <summary>
    /// GitHub Models, Google Gemini e OpenAI aceitam o mesmo formato de API,
    /// então usamos o mesmo cliente e trocamos só o endereço e a chave.
    /// </summary>
    private static IChatClient CriarClienteCompativelComOpenAI(OpcaoProvedor provedor, string modelo, string? chaveDigitada)
    {
        // Nunca coloque a chave no código-fonte! Ela vem da tela ou de uma variável de ambiente.
        var chave = string.IsNullOrWhiteSpace(chaveDigitada)
            ? Environment.GetEnvironmentVariable(provedor.VariavelDeAmbienteChave!)
            : chaveDigitada.Trim();

        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new InvalidOperationException(
                $"Informe a chave de API do provedor \"{provedor.Nome}\" no campo da barra lateral.\n\n" +
                $"Como obter: {provedor.ComoObterChave}");
        }

        var opcoes = new OpenAIClientOptions();
        if (provedor.Endpoint is not null)
            opcoes.Endpoint = new Uri(provedor.Endpoint);

        return new OpenAI.Chat.ChatClient(modelo, new ApiKeyCredential(chave), opcoes).AsIChatClient();
    }
}
