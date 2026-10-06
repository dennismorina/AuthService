namespace AuthService.Application.Exceptions;

public sealed class ValidationException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class AuthenticationFailedException(string message) : Exception(message);
public sealed class ConcurrencyConflictException(string message, Exception? innerException = null) : Exception(message, innerException);
