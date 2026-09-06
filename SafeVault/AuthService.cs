public sealed record AuthenticatedUser(string Username, string Role);

public sealed class AuthService
{
    private readonly UserRepository _users;

    public AuthService(UserRepository users)
    {
        _users = users;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var credentials = await _users.FindCredentialsAsync(username.Trim());
        if (credentials is null || !BCrypt.Net.BCrypt.Verify(password, credentials.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUser(credentials.Username, credentials.Role);
    }

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}