using BCrypt.Net;
using Microsoft.Data.Sqlite;

namespace SecurePrint.BackendService;

public sealed class UserStore
{
    private readonly Database _db;
    private readonly ILogger<UserStore> _logger;

    public UserStore(Database db, ILogger<UserStore> logger)
    {
        _db = db;
        _logger = logger;

        // Ensure at least one default user exists for first-run testing.
        EnsureDefaultUser();
    }

    public bool ValidateCredentials(string username, string password, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            error = "Username and password are required.";
            return false;
        }

        using var conn = _db.Open();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT password_hash FROM users WHERE username = $u;";
        cmd.Parameters.AddWithValue("$u", username);
        var hash = cmd.ExecuteScalar() as string;
        if (hash is null)
        {
            error = "User not found.";
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, hash))
        {
            error = "Invalid password.";
            return false;
        }

        return true;
    }

    public void UpsertUser(string username, string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        using var conn = _db.Open();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        INSERT INTO users(username, password_hash, created_utc)
        VALUES ($u, $h, $c)
        ON CONFLICT(username) DO UPDATE SET password_hash = excluded.password_hash;
        """;
        cmd.Parameters.AddWithValue("$u", username);
        cmd.Parameters.AddWithValue("$h", hash);
        cmd.Parameters.AddWithValue("$c", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        _logger.LogInformation("Upserted user {user}", username);
    }

    private void EnsureDefaultUser()
    {
        using var conn = _db.Open();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM users;";
        var count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
        if (count > 0) return;

        // Default: admin / admin123 (change after setup)
        UpsertUser("admin", "admin123");
        _logger.LogWarning("Created default user admin/admin123. Change this after setup.");
    }
}

