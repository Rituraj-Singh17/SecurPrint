using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using SecurePrint.Shared;
using System.Text.Json;

namespace SecurePrint.UserAgent;

public partial class AuthWindow : Window, INotifyPropertyChanged
{
    private readonly InterceptedJobInfo _job;
    private readonly DispatcherTimer _timer;

    private string _password = "";
    private string _username = "";
    private string _countdownText = "";
    private string _errorText = "";

    private bool _isClosingHandled = false;

    public AuthSubmission? Submission { get; private set; }

    public string DocumentName => _job.DocumentName;
    public string SubmittingUser => _job.SubmittingUser;
    public string PrinterName => _job.PrinterName;

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string CountdownText
    {
        get => _countdownText;
        set { _countdownText = value; OnPropertyChanged(); }
    }

    public string ErrorText
    {
        get => _errorText;
        set { _errorText = value; OnPropertyChanged(); }
    }

    public AuthWindow(InterceptedJobInfo job)
    {
        _job = job;
        InitializeComponent();
        DataContext = this;

        Username = job.SubmittingUser;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateCountdown();
        _timer.Start();

        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        var remaining = _job.DeadlineUtc - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            _timer.Stop();

            CountdownText = "Timed out. Cancelling job...";

            Submission = new AuthSubmission(
                _job.JobId,
                _job.PrinterName,
                Username ?? "",
                Password: "",
                Cancel: true
            );

            _isClosingHandled = true;
            DialogResult = false;
            Close();
            return;
        }

        CountdownText = $"Time remaining: {remaining:mm\\:ss}";
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _password = PasswordBox.Password;
    }

    // UPDATED WITH VALIDATION (ONLY CHANGE)
    private void Release_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(_password))
        {
            ErrorText = "Enter username and password.";
            return;
        }

        try
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json");
            var json = System.IO.File.ReadAllText(path);
            var users = JsonSerializer.Deserialize<List<User>>(json);

            bool valid = users != null &&
                         users.Any(u =>
                             u.Username == Username.Trim() &&
                             u.Password == _password);

            if (!valid)
            {
                ErrorText = "Invalid username or password";
                return;
            }
        }
        catch
        {
            ErrorText = "User database error";
            return;
        }

        _timer.Stop();

        Submission = new AuthSubmission(
            _job.JobId,
            _job.PrinterName,
            Username.Trim(),
            _password,
            Cancel: false
        );

        _isClosingHandled = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();

        Submission = new AuthSubmission(
            _job.JobId,
            _job.PrinterName,
            Username.Trim(),
            Password: "",
            Cancel: true
        );

        _isClosingHandled = true;
        DialogResult = false;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _timer.Stop();

        if (!_isClosingHandled)
        {
            Submission = new AuthSubmission(
                _job.JobId,
                _job.PrinterName,
                Username ?? "",
                Password: "",
                Cancel: true
            );
        }

        base.OnClosing(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // SIMPLE USER MODEL (NO EXTRA FILE NEEDED)
    private class User
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}