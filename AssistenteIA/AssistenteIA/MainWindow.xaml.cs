using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AssistenteIA.ViewModels;

namespace AssistenteIA;

/// <summary>
/// Code-behind com apenas comportamento puramente visual
/// (rolagem e foco). Toda a lógica fica no MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CaixaEntrada.Focus();
    }

    // O PasswordBox não permite binding (por segurança), então repassamos a chave manualmente.
    private void CaixaChave_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ChaveApi = CaixaChave.Password;
    }

    // Ao trocar de provedor, limpa a chave (cada provedor tem a sua).
    private void Provedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count > 0)
            CaixaChave.Clear();
    }

    // Rola automaticamente para o fim quando chega texto novo.
    private void RolagemChat_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0)
            RolagemChat.ScrollToEnd();
    }

    // Os TextBox das mensagens "engolem" a rolagem do mouse; aqui repassamos para o ScrollViewer.
    private void RolagemChat_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        RolagemChat.ScrollToVerticalOffset(RolagemChat.VerticalOffset - e.Delta / 3.0);
        e.Handled = true;
    }
}
