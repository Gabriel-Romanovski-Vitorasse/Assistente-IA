using System.Windows;
using System.Windows.Threading;
using AssistenteIA.Services;
using AssistenteIA.ViewModels;

namespace AssistenteIA;

public partial class App : Application
{
    /// <summary>
    /// "Composition root": é aqui que as dependências são montadas
    /// e entregues ao ViewModel (injeção de dependência manual).
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += AoOcorrerErroNaoTratado;

        var viewModel = new MainViewModel(new ServicoDialogo());
        var janela = new MainWindow { DataContext = viewModel };
        janela.Show();
    }

    private void AoOcorrerErroNaoTratado(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Erro inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
