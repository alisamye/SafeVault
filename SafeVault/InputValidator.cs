using System.Net.Mail;
using System.Text.RegularExpressions;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static partial class InputValidator
{
    private static readonly Regex UsernamePattern = UsernameRegex();

    public static ValidationResult Validate(string? username, string? email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username) || !UsernamePattern.IsMatch(username.Trim()))
        {
            errors.Add("Username must contain 3-50 letters, numbers, dots, underscores, or hyphens.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required.");
        }
        else
        {
            try
            {
                var address = new MailAddress(email.Trim());
                if (!string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Email is invalid.");
                }
            }
            catch (FormatException)
            {
                errors.Add("Email is invalid.");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernameRegex();
}