using System.Drawing;
using System.Windows.Forms;

namespace SecurePrint.UserAgent;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _diagnosticsItem;

    public Func<(string Status, string? LastJob, string? LastError)>? DiagnosticsProvider { get; set; }

    public TrayIcon()
    {
        _icon = new NotifyIcon
        {
            Text = "SecurePrint User Agent",
            Visible = true,
            Icon = SystemIcons.Shield
        };

        var menu = new ContextMenuStrip();
        _statusItem = new ToolStripMenuItem("Status: starting...");
        menu.Items.Add(_statusItem);
        _diagnosticsItem = new ToolStripMenuItem("Connection Diagnostics", null, (_, _) => ShowDiagnostics());
        menu.Items.Add(_diagnosticsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()));
        _icon.ContextMenuStrip = menu;

        _icon.DoubleClick += (_, _) =>
        {
            _icon.ShowBalloonTip(1500, "SecurePrint", "Running in system tray.", ToolTipIcon.Info);
        };
    }

    public void SetStatus(string status)
    {
        if (_icon.ContextMenuStrip is null) return;
        if (_icon.ContextMenuStrip.Items.Count == 0) return;
        _statusItem.Text = $"Status: {status}";
        _icon.Text = $"SecurePrint User Agent - {status}".Length > 63
            ? "SecurePrint User Agent"
            : $"SecurePrint User Agent - {status}";
    }

    public void Notify(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _icon.ShowBalloonTip(2000, title, message, icon);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private void ShowDiagnostics()
    {
        var diag = DiagnosticsProvider?.Invoke() ?? ("unknown", null, null);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var w = new DiagnosticsWindow(diag.Status, diag.LastJob, diag.LastError)
            {
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Topmost = true
            };
            w.Show();
            w.Activate();
        });
    }
}

