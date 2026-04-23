using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after response started.");
                throw;
            }

            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var (status, problem) = MapException(ex);
            problem.Instance = context.Request.Path;
            if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var cid) && cid is string s)
                problem.Extensions["correlationId"] = s;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, JsonOptions);
        }
    }

    private static (int status, ProblemDetails problem) MapException(Exception ex) => ex switch
    {
        ValidationException vx => (
            (int)HttpStatusCode.BadRequest,
            new ProblemDetails
            {
                Title = "Validation failed",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "One or more validation errors occurred.",
                Extensions =
                {
                    ["errors"] = vx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            }),
        ArgumentException ax => (
            (int)HttpStatusCode.BadRequest,
            new ProblemDetails
            {
                Title = "Bad request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = ax.Message
            }),
        InvalidOperationException ix => (
            (int)HttpStatusCode.Conflict,
            new ProblemDetails
            {
                Title = "Conflict",
                Status = (int)HttpStatusCode.Conflict,
                Detail = ix.Message
            }),
        UnauthorizedAccessException ux => (
            (int)HttpStatusCode.Forbidden,
            new ProblemDetails
            {
                Title = "Forbidden",
                Status = (int)HttpStatusCode.Forbidden,
                Detail = ux.Message
            }),
        KeyNotFoundException kx => (
            (int)HttpStatusCode.NotFound,
            new ProblemDetails
            {
                Title = "Not found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = kx.Message
            }),
        DbUpdateException dbx when IsPostgresUniqueViolation(dbx) => (
            (int)HttpStatusCode.Conflict,
            new ProblemDetails
            {
                Title = "Conflict",
                Status = (int)HttpStatusCode.Conflict,
                Detail = UniqueViolationDetail(dbx)
            }),
        _ => (
            (int)HttpStatusCode.InternalServerError,
            new ProblemDetails
            {
                Title = "Server error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An unexpected error occurred."
            })
    };

    /// <summary>23505 / unique index — e.g. duplicate email under concurrency without API-level check.</summary>
    private static bool IsPostgresUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("23505", StringComparison.Ordinal))
                return true;
            if (inner.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                && inner.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string UniqueViolationDetail(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase))
                return "This email is already registered.";
            if (inner.Message.Contains("IX_BusOperators_Email", StringComparison.OrdinalIgnoreCase))
                return "This operator email is already registered.";
        }

        return "A unique value conflict occurred. The record may already exist.";
    }
}
