using EforTakip.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            Application.Common.Exceptions.ValidationException validationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Doğrulama hatası",
                Detail = validationException.Message,
                Extensions = { ["errors"] = validationException.Errors }
            },
            NotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Kayıt bulunamadı",
                Detail = notFoundException.Message
            },
            BusinessRuleValidationException businessRuleException => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "İş kuralı ihlali",
                Detail = businessRuleException.Message
            },
            Application.Common.Exceptions.AuthenticationFailedException authenticationFailedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Kimlik doğrulama başarısız",
                Detail = authenticationFailedException.Message
            },
            // Dizin sunucusuna ulaşılamaması bizim değil, üst sistemin hatasıdır.
            Application.Common.Exceptions.DirectoryConnectionException directoryConnectionException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Dizin sunucusuna bağlanılamadı",
                Detail = directoryConnectionException.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata oluştu",
                Detail = "İşleminiz gerçekleştirilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            }
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Beklenmeyen bir hata oluştu. CorrelationId: {CorrelationId}",
                httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        }
        else
        {
            logger.LogWarning(exception, "İstek işlenirken hata oluştu: {Title}", problemDetails.Title);
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
