using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecurePrint.Shared;

public static class MessageTypes
{
    public const string JobIntercepted = "job_intercepted";
    public const string AuthResult = "auth_result";
    public const string AuthDecision = "auth_decision";
    public const string Ack = "ack";
}

public sealed record PipeEnvelope(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record Ack(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("error")] string? Error);

public sealed record InterceptedJobInfo(
    [property: JsonPropertyName("jobId")] int JobId,
    [property: JsonPropertyName("printerName")] string PrinterName,
    [property: JsonPropertyName("documentName")] string DocumentName,
    [property: JsonPropertyName("submittingUser")] string SubmittingUser,
    [property: JsonPropertyName("submittedAtUtc")] DateTime SubmittedAtUtc,
    [property: JsonPropertyName("deadlineUtc")] DateTime DeadlineUtc);

public sealed record AuthSubmission(
    [property: JsonPropertyName("jobId")] int JobId,
    [property: JsonPropertyName("printerName")] string PrinterName,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("cancel")] bool Cancel);

public sealed record AuthDecision(
    [property: JsonPropertyName("jobId")] int JobId,
    [property: JsonPropertyName("printerName")] string PrinterName,
    [property: JsonPropertyName("approved")] bool Approved,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("error")] string? Error);

public static class PipeCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(Stream stream, string type, string messageId, string? correlationId, object payload, CancellationToken ct)
    {
        var payloadEl = JsonSerializer.SerializeToElement(payload, JsonOptions);
        var env = new PipeEnvelope(type, messageId, correlationId, payloadEl);
        var json = JsonSerializer.SerializeToUtf8Bytes(env, JsonOptions);

        var len = BitConverter.GetBytes(json.Length);
        await stream.WriteAsync(len, ct);
        await stream.WriteAsync(json, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<PipeEnvelope?> ReadAsync(Stream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        if (!await ReadExactAsync(stream, lenBuf, ct)) return null;
        var len = BitConverter.ToInt32(lenBuf, 0);
        if (len <= 0 || len > 10_000_000) throw new InvalidDataException($"Invalid frame length: {len}");

        var payload = new byte[len];
        if (!await ReadExactAsync(stream, payload, ct)) return null;
        return JsonSerializer.Deserialize<PipeEnvelope>(payload, JsonOptions);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (read == 0) return false;
            offset += read;
        }
        return true;
    }
}

