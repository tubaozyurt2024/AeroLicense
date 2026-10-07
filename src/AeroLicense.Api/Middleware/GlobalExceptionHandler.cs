using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AeroLicense.Api.Middleware;

/// <summary>
/// Tüm istisnaları tek yerde RFC 9457 ProblemDetails yanıtına çevirir. Beklenmeyen hatalarda
/// istemciye sadece genel mesaj + traceId gider; stack trace ve iç detay sadece log'a yazılır.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // İstemci bağlantıyı kapattıysa (CancellationToken iptal) hata değil; sadece kaydet.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("İstek istemci tarafından iptal edildi.");
            httpContext.Response.StatusCode = 499;
            return true;
        }

        var (status, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Doğrulama hatası", "Bir veya daha fazla alan geçersiz."),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "Kimlik doğrulanamadı", e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, "Yetkisiz işlem", e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, "Kayıt bulunamadı", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Çakışma", e.Message),
            // İyimser eşzamanlılık: kayıt okunduktan sonra başka biri değiştirdi (ör. iki denetçi aynı anda karar verdi).
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Çakışma", "Kayıt siz işlem yaparken değişti. Güncel halini alıp tekrar deneyin."),
            // Servisteki ön kontrolü eşzamanlı iki istek birlikte geçerse unique index yakalar.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Çakışma", "Aynı kayıt zaten mevcut."),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "İş kuralı ihlali", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu", "Sorun devam ederse traceId ile destek ekibine başvurun.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "İşlenmeyen hata");
        else
            logger.LogWarning("İstek reddedildi: {ExceptionType} {Detail}", exception.GetType().Name, detail);

        var problem = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
