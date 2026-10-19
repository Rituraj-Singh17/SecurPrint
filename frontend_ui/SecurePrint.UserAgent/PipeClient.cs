using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using SecurePrint.Shared;

namespace SecurePrint.UserAgent;

public class PipeClient
{
    public event Func<InterceptedJobInfo, Task>? JobReceived;

    private NamedPipeClientStream? _client;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Connecting to backend pipe...");

                _client = new NamedPipeClientStream(
                    ".",
                    "SecurePrintPipe",
                    PipeDirection.InOut,   // FIXED
                    PipeOptions.Asynchronous);

                await _client.ConnectAsync(5000, ct);

                Console.WriteLine("CONNECTED TO BACKEND");

                var buffer = new byte[8192];

                while (_client.IsConnected && !ct.IsCancellationRequested)
                {
                    int bytesRead = await _client.ReadAsync(buffer, 0, buffer.Length, ct);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Pipe disconnected");
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($"RECEIVED: {json}");

                    try
                    {
                        var job = JsonSerializer.Deserialize<InterceptedJobInfo>(json);

                        if (job != null)
                        {
                            Console.WriteLine($"JOB RECEIVED → {job.JobId}");

                            if (JobReceived != null)
                                await JobReceived.Invoke(job);
                        }
                        else
                        {
                            Console.WriteLine("Deserialize failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON ERROR: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PIPE ERROR: {ex.Message}");
                await Task.Delay(1000, ct);
            }
        }

        Console.WriteLine("Pipe client stopped");
    }

    // SEND RESPONSE BACK TO BACKEND
    public async Task SendAuthSubmissionAsync(AuthSubmission submission, CancellationToken ct)
    {
        if (_client == null || !_client.IsConnected)
        {
            Console.WriteLine("Cannot send → pipe not connected");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(submission);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _client.WriteAsync(bytes, 0, bytes.Length, ct);
            await _client.FlushAsync(ct);

            Console.WriteLine($"Sent auth result → Job {submission.JobId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send failed: {ex.Message}");
        }
    }
}