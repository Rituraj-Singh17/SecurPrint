using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurePrint.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace SecurePrint.UserAgent;

public sealed class UserAgentWorker : BackgroundService
{
    private readonly PipeClient _pipe;
    private readonly ILogger<UserAgentWorker> _logger;

    public UserAgentWorker(
        IOptions<UserAgentOptions> opts,
        TrayIcon tray,
        PipeClient pipe,
        ILogger<UserAgentWorker> logger)
    {
        _pipe = pipe;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserAgent started");

        _pipe.JobReceived += async job =>
        {
            _logger.LogInformation("JOB RECEIVED IN UI {id}", job.JobId);

            //  FIX: initialize properly
            AuthSubmission submission = new AuthSubmission(
                job.JobId,
                job.PrinterName,
                job.SubmittingUser,
                "",
                true
            );

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = new AuthWindow(job)
                {
                    Topmost = true
                };

                window.ShowDialog();

                // overwrite if user provided input
                if (window.Submission != null)
                {
                    submission = window.Submission;
                }
            });

            _logger.LogInformation(
                "Sending submission → JobId={id}, Cancel={cancel}",
                submission.JobId,
                submission.Cancel);

            await _pipe.SendAuthSubmissionAsync(submission, stoppingToken);
        };

        await _pipe.RunAsync(stoppingToken);
    }
}