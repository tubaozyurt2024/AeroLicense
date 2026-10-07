namespace AeroLicense.Application.Common.Exceptions;

// Her istisna türü API katmanında tek bir HTTP durum koduna eşlenir (GlobalExceptionHandler).
public sealed class NotFoundException(string message) : Exception(message);        // 404
public sealed class ForbiddenException(string message) : Exception(message);       // 403
public sealed class ConflictException(string message) : Exception(message);        // 409
public sealed class UnauthorizedException(string message) : Exception(message);    // 401
