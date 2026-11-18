using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using AssistenteIA.Ferramentas;
using AssistenteIA.Infra;
using AssistenteIA.Models;
using AssistenteIA.Services;

namespace AssistenteIA.ViewModels;

/// <summary>
/// ViewModel da janela principal (padrão MVVM).
/// A View (XAML) só faz binding com estas propriedades e comandos.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private const int LimiteCaracteresArquivo = 8_000;

    private readonly IServicoDialogo _dialogo;
    private readonly FerramentasApp _ferramentas = new();
    private readonly AssistenteService _assistente = new();

    private OpcaoProvedor _provedorSelecionado;
    private string _modelo;
    private bool _usarFerramentas = true;
    private string _textoEntrada = string.Empty;
    private string _status = "Pronto.";
    private bool _ocupado;
    private string _chaveApi = string.Empty;

    private (ProvedorIA Provedor, string Modelo, string Chave, bool Ferramentas)? _configuracaoAtual;
    private CancellationTokenSource? _cancelamento;
    private MensagemChat? _respostaAtual;

    public MainViewModel(IServicoDialogo dialogo)
    {
        _dialogo = dialogo;
        _provedorSelecionado = Provedores[0];
        _modelo = _provedorSelecionado.ModeloPadrao;

        EnviarCommand = new RelayCommand(EnviarAsync, () => !Ocupado && !string.IsNullOrWhiteSpace(TextoEntrada));
        ResumirArquivoCommand = new RelayCommand(ResumirArquivoAsync, () => !Ocupado);
        LimparCommand = new RelayCommand(Limpar, () => !Ocupado);
        CancelarCommand = new RelayCommand(() => _cancelamento?.Cancel(), () => Ocupado);

        _ferramentas.FerramentaExecutada += AoExecutarFerramenta;
        Mensagens.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SemMensagens));
    }

    // ---------- Propriedades usadas pela tela ----------

    public ObservableCollection<MensagemChat> Mensagens { get; } = [];

    public IReadOnlyList<OpcaoProvedor> Provedores => OpcaoProvedor.Todas;

    public IReadOnlyList<Produto> Estoque => _ferramentas.Produtos;

    public bool SemMensagens => Mensagens.Count == 0;

    public OpcaoProvedor ProvedorSelecionado
    {
        get => _provedorSelecionado;
        set
        {
            if (SetProperty(ref _provedorSelecionado, value))
                Modelo = value.ModeloPadrao; // sugere o modelo padrão do provedor
        }
    }

    public string Modelo
    {
        get => _modelo;
        set => SetProperty(ref _modelo, value);
    }

    /// <summary>
    /// Chave digitada na tela. Fica só na memória (não é salva em disco).
    /// </summary>
    public string ChaveApi
    {
        get => _chaveApi;
        set => SetProperty(ref _chaveApi, value);
    }

    public bool UsarFerramentas
    {
        get => _usarFerramentas;
        set => SetProperty(ref _usarFerramentas, value);
    }

    public string TextoEntrada
    {
        get => _textoEntrada;
        set => SetProperty(ref _textoEntrada, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool Ocupado
    {
        get => _ocupado;
        private set
        {
            if (SetProperty(ref _ocupado, value))
            {
                OnPropertyChanged(nameof(NaoOcupado));
                CommandManager.InvalidateRequerySuggested(); // reavalia os botões
            }
        }
    }

    public bool NaoOcupado => !Ocupado;

    // ---------- Comandos ----------

    public ICommand EnviarCommand { get; }
    public ICommand ResumirArquivoCommand { get; }
    public ICommand LimparCommand { get; }
    public ICommand CancelarCommand { get; }

    private async Task EnviarAsync()
    {
        var texto = TextoEntrada.Trim();
        if (texto.Length == 0 || Ocupado)
            return;

        TextoEntrada = string.Empty;
        Mensagens.Add(MensagemChat.DoUsuario(texto));
        await ResponderAsync(texto);
    }

    private async Task ResumirArquivoAsync()
    {
        var caminho = _dialogo.SelecionarArquivoTexto();
        if (caminho is null)
            return;

        string conteudo;
        try
        {
            conteudo = await File.ReadAllTextAsync(caminho);
        }
        catch (Exception ex)
        {
            Mensagens.Add(MensagemChat.DeErro($"Não foi possível ler o arquivo: {ex.Message}"));
            return;
        }

        // Modelos têm um limite de contexto (tokens), então cortamos textos muito grandes.
        var truncado = conteudo.Length > LimiteCaracteresArquivo;
        if (truncado)
            conteudo = conteudo[..LimiteCaracteresArquivo];

        var nomeArquivo = Path.GetFileName(caminho);
        Mensagens.Add(MensagemChat.DoUsuario(
            $"📄 Resumir o arquivo \"{nomeArquivo}\" ({conteudo.Length:N0} caracteres{(truncado ? ", truncado" : "")})"));

        // Engenharia de prompt: instrução clara + delimitação do conteúdo.
        var prompt = $"""
            Resuma o documento abaixo em português, em no máximo 5 tópicos curtos.
            Em seguida, diga em uma única frase qual é o assunto principal.
            Depois disso, o usuário poderá fazer perguntas sobre esse documento.

            --- INÍCIO DO DOCUMENTO "{nomeArquivo}" ---
            {conteudo}
            --- FIM DO DOCUMENTO ---
            """;

        await ResponderAsync(prompt);
    }

    private void Limpar()
    {
        Mensagens.Clear();
        _assistente.LimparHistorico();
        Status = "Conversa limpa.";
    }

    // ---------- Lógica principal ----------

    private async Task ResponderAsync(string promptParaIA)
    {
        if (!PrepararCliente())
            return;

        _respostaAtual = MensagemChat.DoAssistente($"Assistente · {ProvedorSelecionado.Nome} ({Modelo.Trim()})");
        Mensagens.Add(_respostaAtual);

        Ocupado = true;
        Status = "Gerando resposta...";
        _cancelamento = new CancellationTokenSource();
        var cronometro = Stopwatch.StartNew();

        try
        {
            // async/await: a interface continua respondendo enquanto a IA gera o texto.
            await foreach (var trecho in _assistente.EnviarAsync(promptParaIA, _cancelamento.Token))
                _respostaAtual.Texto += trecho;

            if (_respostaAtual.Texto.Length == 0)
                _respostaAtual.Texto = "(o modelo não retornou texto)";

            Status = $"Resposta gerada em {cronometro.Elapsed.TotalSeconds:F1} s.";
        }
        catch (OperationCanceledException)
        {
            _respostaAtual.Texto += (_respostaAtual.Texto.Length > 0 ? "\n\n" : "") + "⏹ Resposta interrompida.";
            Status = "Geração interrompida pelo usuário.";
        }
        catch (Exception ex)
        {
            _respostaAtual.MarcarComoErro(DescreverErro(ex));
            Status = "Ocorreu um erro.";
        }
        finally
        {
            _cancelamento.Dispose();
            _cancelamento = null;
            _respostaAtual = null;
            Ocupado = false;
        }
    }

    /// <summary>
    /// Cria (ou recria) o cliente de IA apenas quando a configuração muda.
    /// </summary>
    private bool PrepararCliente()
    {
        var modelo = Modelo.Trim();
        if (modelo.Length == 0)
        {
            Status = "Informe o nome do modelo.";
            return false;
        }

        var configuracao = (ProvedorSelecionado.Tipo, modelo, ChaveApi.Trim(), UsarFerramentas);
        if (_configuracaoAtual == configuracao)
            return true;

        try
        {
            var cliente = ChatClientFactory.Criar(ProvedorSelecionado, modelo, ChaveApi, UsarFerramentas);
            _assistente.TrocarCliente(cliente, UsarFerramentas ? _ferramentas.ComoFerramentasDeIA() : null);
            _configuracaoAtual = configuracao;
            return true;
        }
        catch (Exception ex)
        {
            Mensagens.Add(MensagemChat.DeErro(ex.Message));
            Status = "Não foi possível configurar o provedor.";
            return false;
        }
    }

    private void AoExecutarFerramenta(string chamada)
    {
        // A ferramenta pode rodar em outra thread; a tela só pode ser alterada pela thread da UI.
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var aviso = MensagemChat.DeFerramenta($"🔧 A IA chamou a ferramenta {chamada}");
            var indice = _respostaAtual is null ? -1 : Mensagens.IndexOf(_respostaAtual);

            if (indice >= 0)
                Mensagens.Insert(indice, aviso); // mostra antes da resposta
            else
                Mensagens.Add(aviso);
        });
    }

    private string DescreverErro(Exception ex)
    {
        var mensagem = ex.Message;
        var modelo = Modelo.Trim();

        if (ProvedorSelecionado.Tipo == ProvedorIA.Ollama)
        {
            if (ex is HttpRequestException || ex.InnerException is HttpRequestException)
            {
                return $"Não foi possível conectar ao Ollama em {ChatClientFactory.UrlOllama}.\n" +
                       "Verifique se ele está instalado e em execução (abra o app Ollama ou rode \"ollama serve\" no terminal).";
            }

            if (mensagem.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return $"O modelo \"{modelo}\" não foi encontrado no Ollama.\n" +
                       $"Baixe-o no terminal com:  ollama pull {modelo}";
            }

            if (mensagem.Contains("does not support tools", StringComparison.OrdinalIgnoreCase))
            {
                return $"O modelo \"{modelo}\" não suporta ferramentas (function calling).\n" +
                       "Desmarque a opção de ferramentas ou use um modelo como llama3.2 ou qwen2.5.";
            }
        }

        if (mensagem.Contains("401") || mensagem.Contains("403") ||
            mensagem.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "A chave de API foi recusada (inválida, expirada ou sem permissão).\n" +
                   $"Como obter uma chave válida: {ProvedorSelecionado.ComoObterChave}";
        }

        if (mensagem.Contains("429") || mensagem.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return "O limite de uso gratuito foi atingido. Aguarde um pouco e tente de novo, " +
                   "ou troque de provedor (por exemplo, do GitHub Models para o Gemini).";
        }

        if (mensagem.Contains("503") || mensagem.Contains("overloaded", StringComparison.OrdinalIgnoreCase))
        {
            return "Os servidores do provedor estão sobrecarregados no momento (erro 503). " +
                   "Isso é comum em planos gratuitos. Tente de novo em alguns minutos ou troque de provedor.";
        }

        if (mensagem.Contains("unknown_model", StringComparison.OrdinalIgnoreCase) || mensagem.Contains("404"))
        {
            return $"O provedor não encontrou o modelo \"{modelo}\" (erro 404). Ele pode ter sido descontinuado " +
                   "ou o nome está errado. Confira o nome atual na documentação do provedor e digite no campo Modelo.";
        }

        if (ex is HttpRequestException || ex.InnerException is HttpRequestException)
        {
            return "Não foi possível acessar o serviço de IA. Verifique a conexão com a internet " +
                   "(a rede da faculdade pode bloquear alguns sites).";
        }

        return $"Ocorreu um erro ao falar com a IA:\n{mensagem}";
    }
}
