using Microsoft.Data.Sqlite;

public sealed class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL DEFAULT '',
                Role TEXT NOT NULL DEFAULT 'user'
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddAsync(string username, string email)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Username, Email) VALUES ($username, $email);";
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$email", email);
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddUserAsync(string username, string email, string passwordHash, string role = "user")
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES ($username, $email, $passwordHash, $role);";
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$role", role);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<UserCredentials?> FindCredentialsAsync(string username)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Username, PasswordHash, Role FROM Users WHERE Username = $username LIMIT 1;";
        command.Parameters.AddWithValue("$username", username);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserCredentials(reader.GetString(0), reader.GetString(1), reader.GetString(2));
    }
}

public sealed record UserCredentials(string Username, string PasswordHash, string Role);