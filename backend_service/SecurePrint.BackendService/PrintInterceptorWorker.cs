using System.Collections.Concurrent;
using System.Printing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurePrint.Shared;
using System.Threading;

namespace SecurePrint.BackendService;

public sealed class PrintInterceptorWorker : BackgroundService
{
    private readonly SecurePrintOptions _opts;
    private readonly ILogger<PrintInterceptorWorker> _logger;
    private readonly NamedPipeHub _pipe;

    private readonly ConcurrentDictionary<int, bool> _seenJobs = new();

    public PrintInterceptorWorker(
        IOptions<SecurePrintOptions> opts,
        ILogger<PrintInterceptorWorker> logger,
        NamedPipeHub pipe)
    {
        _opts = opts.Value;
        _logger = logger;
        _pipe = pipe;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start pipe server
        _ = Task.Run(() => _pipe.RunAsync(stoppingToken), stoppingToken);

        _logger.LogInformation("Monitoring printer: {printer}", _opts.PrinterName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // CRITICAL FIX: create PrintQueue INSIDE loop (thread-safe)
                using var server = new LocalPrintServer();
                using var queue = server.GetPrintQueue(_opts.PrinterName);

                queue.Refresh();

                var jobs = queue.GetPrintJobInfoCollection();

                foreach (var job in jobs)
                {
                    try
                    {
                        job.Refresh();

                        // Skip already handled jobs
                        if (_seenJobs.ContainsKey(job.JobIdentifier))
                            continue;

                        _logger.LogInformation("Intercepting Job {id}", job.JobIdentifier);

                        bool paused = false;

                        // AGGRESSIVE PAUSE
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                job.Pause();
                                Thread.Sleep(20);
                                job.Refresh();

                                if (job.IsPaused)
                                {
                                    paused = true;
                                    break;
                                }
                            }
                            catch { }
                        }

                        // If too fast → cancel
                        if (!paused)
                        {
                            try
                            {
                                job.Cancel();
                                _logger.LogWarning("Too fast → cancelled job {id}", job.JobIdentifier);
                            }
                            catch { }

                            continue;
                        }

                        // Mark AFTER successful pause
                        _seenJobs[job.JobIdentifier] = true;

                        _logger.LogInformation("Job paused successfully {id}", job.JobIdentifier);

                        // Send job to UI
                        var info = new InterceptedJobInfo(
                            job.JobIdentifier,
                            _opts.PrinterName,
                            job.Name ?? "Unknown",
                            job.Submitter ?? "Unknown",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddSeconds(_opts.AuthTimeoutSeconds)
                        );

                        await _pipe.EnqueueJobAsync(info, stoppingToken);

                        // Timeout auto cancel
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(_opts.AuthTimeoutSeconds * 1000, stoppingToken);

                                job.Refresh();

                                if (job.IsPaused)
                                {
                                    _logger.LogInformation("Timeout → cancelling job {id}", job.JobIdentifier);
                                    job.Cancel();
                                }
                            }
                            catch { }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Job processing error");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitor loop error");
            }

            // FAST polling
            await Task.Delay(50, stoppingToken);
        }
    }
}