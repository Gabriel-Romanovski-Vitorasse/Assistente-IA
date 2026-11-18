using AssistenteIA.Infra;

namespace AssistenteIA.Models;

public enum TipoMensagem
{
    Usuario,
    Assistente,
    Ferramenta,
    Erro
}

/// <summary>
/// Representa uma mensagem exibida na tela do chat.
/// O texto é observável porque a resposta da IA chega aos poucos (streaming).
/// </summary>
public sealed class MensagemChat : ObservableObject
{
    private string _texto;
    private TipoMensagem _tipo;

    private MensagemChat(TipoMensagem tipo, string autor, string texto)
    {
        _tipo = tipo;
        _texto = texto;
        Autor = autor;
    }

    public string Autor { get; }

    public TipoMensagem Tipo
    {
        get => _tipo;
        private set => SetProperty(ref _tipo, value);
    }

    public string Texto
    {
        get => _texto;
        set => SetProperty(ref _texto, value);
    }

    // "Métodos fábrica": deixam claro, no código, que tipo de mensagem está sendo criada.
    public static MensagemChat DoUsuario(string texto) => new(TipoMensagem.Usuario, "Você", texto);
    public static MensagemChat DoAssistente(string autor) => new(TipoMensagem.Assistente, autor, string.Empty);
    public static MensagemChat DeFerramenta(string texto) => new(TipoMensagem.Ferramenta, "Ferramenta", texto);
    public static MensagemChat DeErro(string texto) => new(TipoMensagem.Erro, "Erro", texto);

    public void MarcarComoErro(string texto)
    {
        Tipo = TipoMensagem.Erro;
        Texto = texto;
    }
}
