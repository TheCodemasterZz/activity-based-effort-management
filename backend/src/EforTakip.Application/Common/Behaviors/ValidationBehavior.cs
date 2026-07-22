using FluentValidation;
using MediatR;

namespace EforTakip.Application.Common.Behaviors;

/// <summary>
/// NOT: Kısıt bilinçli olarak <c>notnull</c>; <c>IRequest&lt;TResponse&gt;</c> kullanılırsa
/// yanıt döndürmeyen (<c>IRequest</c>) komutlar için kısıt sağlanamaz ve MediatR bu behavior'ı
/// atlar — o komutlarda doğrulama sessizce çalışmaz.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
            throw new Exceptions.ValidationException(failures);

        return await next();
    }
}
