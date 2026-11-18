using Microsoft.Win32;

namespace AssistenteIA.Services;

/// <summary>
/// Abstrai as janelas de diálogo para que o ViewModel não dependa
/// diretamente de componentes visuais (facilita testes e respeita o MVVM).
/// </summary>
public interface IServicoDialogo
{
    string? SelecionarArquivoTexto();
}

public sealed class ServicoDialogo : IServicoDialogo
{
    public string? SelecionarArquivoTexto()
    {
        var dialogo = new OpenFileDialog
        {
            Title = "Escolha um arquivo de texto para a IA resumir",
            Filter = "Arquivos de texto (*.txt;*.md)|*.txt;*.md|Todos os arquivos (*.*)|*.*"
        };

        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }
}
