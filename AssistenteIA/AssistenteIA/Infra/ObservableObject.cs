using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AssistenteIA.Infra;

/// <summary>
/// Classe base que implementa INotifyPropertyChanged.
/// Permite que a interface (XAML) seja atualizada automaticamente
/// quando uma propriedade muda (base do Data Binding do WPF).
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? nomePropriedade = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomePropriedade));

    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nomePropriedade = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        OnPropertyChanged(nomePropriedade);
        return true;
    }
}
