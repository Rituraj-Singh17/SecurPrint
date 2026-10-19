using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SecurePrint.UserAgent;

public partial class DiagnosticsWindow : Window, INotifyPropertyChanged
{
    public string Status { get; }
    public string LastJobEvent { get; }
    public string LastError { get; }

    public DiagnosticsWindow(string status, string? lastJobEvent, string? lastError)
    {
        Status = status;
        LastJobEvent = lastJobEvent ?? "(none)";
        LastError = lastError ?? "(none)";
        InitializeComponent();
        DataContext = this;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

