using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BusBooking.API.Filters;

/// <summary>
/// Runs FluentValidation for action arguments that have a registered <see cref="IValidator{T}"/>.
/// </summary>
public sealed class ApiDtoValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext?.RequestServices
            ?? throw new InvalidOperationException("HttpContext is not available.");
        foreach (var arg in context.ActionArguments.Values.Where(v => v != null))
        {
            if (arg is CancellationToken)
                continue;

            var type = arg!.GetType();
            if (!type.IsClass || type == typeof(string))
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            var validatorObj = services.GetService(validatorType);
            if (validatorObj is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(arg);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Request validation failed.",
                    Instance = context.HttpContext?.Request.Path.Value,
                    Extensions = { ["errors"] = errors }
                });
                return;
            }
        }

        await next();
    }
}
