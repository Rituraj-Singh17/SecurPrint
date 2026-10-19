using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.IO;

namespace SecurePrint.BackendService;

public sealed class Database
{
    private readonly SecurePrintOptions _opts;
    private readonly ILogger<Database> _logger;

    public Database(IOptions<SecurePrintOptions> opts, ILogger<Database> logger)
    {
        _opts = opts.Value;
        _logger = logger;
        EnsureInitialized();
    }

    public SqliteConnection Open()
    {
        var dbPath = _opts.GetDatabasePathAbsolute(AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        return new SqliteConnection($"Data Source={dbPath};Cache=Shared");
    }

    private void EnsureInitialized()
    {
        var dbPath = _opts.GetDatabasePathAbsolute(AppContext.BaseDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var conn = Open();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS users (
            username TEXT PRIMARY KEY,
            password_hash TEXT NOT NULL,
            created_utc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS print_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            job_id INTEGER NOT NULL,
            printer_name TEXT NOT NULL,
            document_name TEXT NOT NULL,
            submitting_user TEXT NOT NULL,
            status TEXT NOT NULL,
            decided_by TEXT,
            decided_utc TEXT NOT NULL,
            details TEXT
        );
        """;
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Database ready at {path}", dbPath);
    }

    public void InsertLog(int jobId, string printer, string document, string submittingUser, string status, string? decidedBy, string? details)
    {
        using var conn = Open();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        INSERT INTO print_logs(job_id, printer_name, document_name, submitting_user, status, decided_by, decided_utc, details)
        VALUES ($job_id, $printer_name, $document_name, $submitting_user, $status, $decided_by, $decided_utc, $details);
        """;
        cmd.Parameters.AddWithValue("$job_id", jobId);
        cmd.Parameters.AddWithValue("$printer_name", printer);
        cmd.Parameters.AddWithValue("$document_name", document);
        cmd.Parameters.AddWithValue("$submitting_user", submittingUser);
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$decided_by", (object?)decidedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$decided_utc", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$details", (object?)details ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}

