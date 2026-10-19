using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using SecurePrint.Shared;
using Microsoft.Extensions.Logging;
using System.Printing;

namespace SecurePrint.BackendService;

public class NamedPipeHub
{
    private readonly ILogger<NamedPipeHub> _logger;
    private NamedPipeServerStream? _server;

    public NamedPipeHub(ILogger<NamedPipeHub> logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _server = new NamedPipeServerStream(
                    "SecurePrintPipe",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                _logger.LogInformation("Waiting for UI to connect...");

                await _server.WaitForConnectionAsync(ct);

                _logger.LogInformation("UI CONNECTED");

                // START LISTENING FOR UI RESPONSES
                _ = Task.Run(() => ListenForResponses(ct), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipe connection failed");
            }
        }
    }

    public async Task EnqueueJobAsync(InterceptedJobInfo job, CancellationToken ct)
    {
        if (_server == null || !_server.IsConnected)
        {
            _logger.LogWarning("UI NOT CONNECTED → job NOT sent");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(job);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _server.WriteAsync(bytes, 0, bytes.Length, ct);
            await _server.FlushAsync(ct);

            _logger.LogInformation("Job sent to UI {id}", job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job");
        }
    }

    // NEW: LISTEN FOR UI RESPONSE
    private async Task ListenForResponses(CancellationToken ct)
    {
        if (_server == null) return;

        var buffer = new byte[4096];

        while (_server.IsConnected && !ct.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await _server.ReadAsync(buffer, 0, buffer.Length, ct);

                if (bytesRead == 0)
                    continue;

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                var submission = JsonSerializer.Deserialize<AuthSubmission>(json);

                if (submission != null)
                {
                    _logger.LogInformation("Received auth for job {id}", submission.JobId);

                    await HandleAuthResultAsync(submission);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from pipe");
            }
        }
    }

    //  FINAL ACTION HANDLER
    private async Task HandleAuthResultAsync(AuthSubmission submission)
    {
        try
        {
            var server = new LocalPrintServer();
            var queue = server.GetPrintQueue(submission.PrinterName);

            queue.Refresh();

            var job = queue.GetPrintJobInfoCollection()
                           .FirstOrDefault(j => j.JobIdentifier == submission.JobId);

            if (job == null)
            {
                _logger.LogWarning("Job not found {id}", submission.JobId);
                return;
            }

            job.Refresh();

            if (submission.Cancel)
            {
                job.Cancel();
                _logger.LogInformation("Job cancelled {id}", submission.JobId);
                return;
            }

            if (job.IsPaused)
            {
                job.Resume();
                _logger.LogInformation("Job resumed {id}", submission.JobId);
            }
            else
            {
                _logger.LogWarning("Job not paused {id}", submission.JobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth handling failed");
        }

        await Task.CompletedTask;
    }
}