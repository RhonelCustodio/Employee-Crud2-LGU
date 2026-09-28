using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace EmployeeCrud.Data;

public sealed class AdminRepository
{
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly string _connectionString;

    public AdminRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 5
        }.ToString();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);

        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        // Id = 1 permits only one admin account in this starter app.
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Admins (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash BLOB NOT NULL,
                PasswordSalt BLOB NOT NULL,
                PasswordIterations INTEGER NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    public bool HasAdmin()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            "SELECT EXISTS(SELECT 1 FROM Admins);";

        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    public void CreateAdmin(string username, string password)
    {
        username = username.Trim();

        // Limit usernames to characters supported by SQLite's
        // built-in case-insensitive comparison.
        if (!Regex.IsMatch(username, @"\A[A-Za-z0-9_.-]{3,50}\z"))
        {
            throw new ArgumentException(
                "Username must be 3–50 characters. " +
                "Use letters, numbers, dots, underscores, or hyphens.");
        }

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 12 ||
            password.Length > 128)
        {
            throw new ArgumentException("Password must contain 12–128 characters.");
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Admins
                (Id, Username, PasswordHash,
                 PasswordSalt, PasswordIterations)
            VALUES
                (1, $username, $hash, $salt, $iterations);
            """;

        command.Parameters.AddWithValue("$username", username);
        command.Parameters.Add("$hash", SqliteType.Blob).Value = hash;
        command.Parameters.Add("$salt", SqliteType.Blob).Value = salt;
        command.Parameters.AddWithValue("$iterations", Iterations);

        command.ExecuteNonQuery();
    }

    public bool ValidateLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password) ||
            username.Length > 50 ||
            password.Length > 128)
        {
            return false;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT PasswordHash, PasswordSalt, PasswordIterations
            FROM Admins
            WHERE Username = $username;
            """;

        command.Parameters.AddWithValue("$username", username.Trim());

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return false;

        byte[] storedHash =
            reader.GetFieldValue<byte[]>(0);

        byte[] storedSalt =
            reader.GetFieldValue<byte[]>(1);

        int storedIterations = reader.GetInt32(2);

        byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            storedSalt,
            storedIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(
            enteredHash,
            storedHash);
    }
}