using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AssistenteIA.Services;

/// <summary>
/// Regra de negócio da conversa: guarda o histórico e conversa com a IA.
/// Depende apenas da abstração IChatClient, e não de um provedor específico.
/// </summary>
public sealed class AssistenteService : IDisposable
{
    private const string PromptDeSistema =
        "Você é um assistente integrado a um aplicativo desktop WPF de demonstração. " +
        "Responda sempre em português do Brasil, de forma clara e objetiva. " +
        "Quando precisar de informações sobre o estoque de produtos ou sobre a data e hora atuais, " +
        "use as ferramentas disponíveis em vez de inventar dados.";

    // Os LLMs não têm memória: a cada pergunta, enviamos o histórico inteiro.
    private readonly List<ChatMessage> _historico = [];
    private IChatClient? _cliente;
    private ChatOptions _opcoes = new();

    public AssistenteService()
    {
        LimparHistorico();
    }

    /// <summary>
    /// Troca o modelo/provedor em uso. O histórico é mantido, então dá para
    /// começar a conversa com a IA local e continuar com a da nuvem.
    /// </summary>
    public void TrocarCliente(IChatClient novoCliente, IList<AITool>? ferramentas)
    {
        _cliente?.Dispose();
        _cliente = novoCliente;
        _opcoes = new ChatOptions { Tools = ferramentas };
    }

    public void LimparHistorico()
    {
        _historico.Clear();
        _historico.Add(new ChatMessage(ChatRole.System, PromptDeSistema));
    }

    /// <summary>
    /// Envia uma mensagem e devolve a resposta em pedaços (streaming),
    /// para que a interface mostre o texto enquanto ele é gerado.
    /// </summary>
    public async IAsyncEnumerable<string> EnviarAsync(
        string mensagem,
        [EnumeratorCancellation] CancellationToken cancelamento = default)
    {
        var cliente = _cliente ?? throw new InvalidOperationException("Nenhum modelo de IA foi configurado.");

        var mensagemUsuario = new ChatMessage(ChatRole.User, mensagem);
        _historico.Add(mensagemUsuario);

        var atualizacoes = new List<ChatResponseUpdate>();
        IEnumerable<ChatMessage>? mensagensDaResposta = null;
        var recebeuTexto = false;

        try
        {
            await foreach (var atualizacao in cliente.GetStreamingResponseAsync(_historico, _opcoes, cancelamento))
            {
                atualizacoes.Add(atualizacao);

                if (!string.IsNullOrEmpty(atualizacao.Text))
                {
                    recebeuTexto = true;
                    yield return atualizacao.Text;
                }
            }

            if (recebeuTexto)
            {
                mensagensDaResposta = atualizacoes.ToChatResponse().Messages;
            }
            else
            {
                // Alguns provedores falham no modo streaming. Plano B: pedir a resposta inteira de uma vez.
                var respostaCompleta = await cliente.GetResponseAsync(_historico, _opcoes, cancelamento);

                if (string.IsNullOrEmpty(respostaCompleta.Text))
                    throw new InvalidOperationException(MontarDiagnostico(atualizacoes, respostaCompleta));

                mensagensDaResposta = respostaCompleta.Messages;
                yield return respostaCompleta.Text;
            }
        }
        finally
        {
            if (mensagensDaResposta is not null)
            {
                // Guarda a resposta (inclusive chamadas de ferramentas) no histórico.
                _historico.AddRange(mensagensDaResposta);
            }
            else
            {
                // Erro ou cancelamento: desfaz a pergunta para o histórico não ficar inconsistente.
                _historico.Remove(mensagemUsuario);
            }
        }
    }

    /// <summary>
    /// Monta um texto explicando o que o provedor devolveu, para ajudar a encontrar o problema.
    /// </summary>
    private static string MontarDiagnostico(List<ChatResponseUpdate> atualizacoes, ChatResponse respostaCompleta)
    {
        static string Descrever(IEnumerable<AIContent> conteudos)
        {
            var partes = conteudos.Select(c => c switch
            {
                ErrorContent erro => $"Erro(\"{erro.Message}\" {erro.ErrorCode})",
                FunctionCallContent chamada => $"ChamadaDeFunção({chamada.Name})",
                _ => c.GetType().Name
            }).ToList();

            return partes.Count == 0 ? "nenhum" : string.Join(", ", partes);
        }

        var motivos = atualizacoes.Select(a => a.FinishReason?.Value).Where(m => m is not null).Distinct();

        return "O provedor respondeu, mas sem nenhum texto.\n\n" +
               "Diagnóstico:\n" +
               $"• Streaming: {atualizacoes.Count} pacote(s); conteúdos: {Descrever(atualizacoes.SelectMany(a => a.Contents))}; " +
               $"término: {string.Join(", ", motivos)}\n" +
               $"• Sem streaming: conteúdos: {Descrever(respostaCompleta.Messages.SelectMany(m => m.Contents))}; " +
               $"término: {respostaCompleta.FinishReason?.Value}";
    }

    public void Dispose() => _cliente?.Dispose();
}
