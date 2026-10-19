namespace SecurePrint.UserAgent;

public sealed class UserAgentOptions
{
    public string PipeName { get; set; } = "SecurePrintPipe";
    public int ReconnectDelayMs { get; set; } = 1500;
    public int ConnectTimeoutMs { get; set; } = 2000;
    public bool EnableAutoStart { get; set; } = true;
}

