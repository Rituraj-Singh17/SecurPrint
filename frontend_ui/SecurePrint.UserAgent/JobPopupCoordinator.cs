using System.Collections.Concurrent;
using SecurePrint.Shared;

namespace SecurePrint.UserAgent;

public sealed class JobPopupCoordinator
{
    private readonly ConcurrentQueue<InterceptedJobInfo> _queue = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly HashSet<(string Printer, int JobId)> _known = new();

    public async Task EnqueueAndHandleAsync(InterceptedJobInfo job, PipeClient pipe, CancellationToken ct)
    {
        lock (_known)
        {
            if (!_known.Add((job.PrinterName, job.JobId)))
                return; // prevent duplicate popups for same job
        }

        _queue.Enqueue(job);
        await DrainAsync(pipe, ct);
    }

    private async Task DrainAsync(PipeClient pipe, CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct)) return;
        try
        {
            while (_queue.TryDequeue(out var job))
            {
                var submission = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var w = new AuthWindow(job)
                    {
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                        Topmost = true
                    };
                    var ok = w.ShowDialog();
                    return w.Submission ?? new AuthSubmission(job.JobId, job.PrinterName, Username: "", Password: "", Cancel: true);
                });

                try
                {
                    await pipe.SendAuthSubmissionAsync(submission, ct);
                }
                catch
                {
                    // If backend is gone, service will timeout/cancel anyway.
                }
                finally
                {
                    lock (_known) _known.Remove((job.PrinterName, job.JobId));
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}

