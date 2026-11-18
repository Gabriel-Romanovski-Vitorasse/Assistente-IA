using System.Windows.Input;

namespace AssistenteIA.Infra;

/// <summary>
/// Implementação simples de ICommand. Liga botões/teclas do XAML
/// a métodos do ViewModel, sem precisar de código no code-behind.
/// Aceita tanto ações síncronas quanto assíncronas (Task).
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<Task> _executar;
    private readonly Func<bool>? _podeExecutar;

    public RelayCommand(Func<Task> executar, Func<bool>? podeExecutar = null)
    {
        _executar = executar;
        _podeExecutar = podeExecutar;
    }

    public RelayCommand(Action executar, Func<bool>? podeExecutar = null)
        : this(() => { executar(); return Task.CompletedTask; }, podeExecutar)
    {
    }

    // O CommandManager do WPF reavalia o CanExecute automaticamente
    // após interações do usuário (cliques, teclas etc.).
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _podeExecutar?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            await _executar();
    }
}
