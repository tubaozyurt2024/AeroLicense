using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AeroLicense.Api.Filters;

/// <summary>
/// Global action filter: action parametrelerinden, kayıtlı bir IValidator&lt;T&gt;'si olan her birini
/// otomatik doğrular. Yeni bir endpoint eklendiğinde validasyonu çağırmayı unutmak mümkün olmaz.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
                throw new ValidationException(result.Errors); // GlobalExceptionHandler → 400
        }

        await next();
    }
}
