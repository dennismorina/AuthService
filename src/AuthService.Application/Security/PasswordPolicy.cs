using AuthService.Application.Exceptions;

namespace AuthService.Application.Security;

public static class PasswordPolicy
{
    public static void Validate(string password, int minimumLength)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < minimumLength)
            throw new ValidationException($"Password must contain at least {minimumLength} characters.");

        if (!password.Any(char.IsUpper))
            throw new ValidationException("Password must contain an uppercase character.");

        if (!password.Any(char.IsLower))
            throw new ValidationException("Password must contain a lowercase character.");

        if (!password.Any(char.IsDigit))
            throw new ValidationException("Password must contain a digit.");

        if (password.All(char.IsLetterOrDigit))
            throw new ValidationException("Password must contain a non-alphanumeric character.");
    }
}
