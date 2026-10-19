using System.IO;

namespace SecurePrint.BackendService;

public sealed class SecurePrintOptions
{
    public string PrinterName { get; set; } = "Microsoft Print to PDF";
    public string PipeName { get; set; } = "SecurePrintPipe";
    public int PollIntervalMs { get; set; } = 250;
    public int PauseRetryCount { get; set; } = 3;
    public int PauseRetryDelayMs { get; set; } = 100;
    public int AuthTimeoutSeconds { get; set; } = 60;
    public int MaxAuthAttempts { get; set; } = 3;
    public string DataDirectory { get; set; } = "..\\..\\data";
    public string DatabaseFile { get; set; } = "secureprint.db";
    public string LogDirectory { get; set; } = "..\\..\\data\\logs";

    public string GetDataDirectoryAbsolute(string baseDir) =>
        Path.GetFullPath(Path.Combine(baseDir, DataDirectory));

    public string GetDatabasePathAbsolute(string baseDir) =>
        Path.Combine(GetDataDirectoryAbsolute(baseDir), DatabaseFile);

    public string GetLogDirectoryAbsolute(string baseDir) =>
        Path.GetFullPath(Path.Combine(baseDir, LogDirectory));
}

