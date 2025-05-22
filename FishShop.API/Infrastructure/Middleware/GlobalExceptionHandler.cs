using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Infrastructure.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var task = exception switch
        {
            DbUpdateConcurrencyException => HandleConcurrencyException(exception, httpContext, cancellationToken),
            DbUpdateException => HandleDbUpdateException(exception, httpContext, cancellationToken),
            UnauthorizedAccessException => HandleUnauthorizedException(exception, httpContext, cancellationToken),
            ArgumentNullException => HandleArgumentNullException(exception, httpContext, cancellationToken),
            ArgumentException => HandleArgumentException(exception, httpContext, cancellationToken),
            KeyNotFoundException => HandleNotFoundException(exception, httpContext, cancellationToken),
            InvalidOperationException => HandleInvalidOperationException(exception, httpContext, cancellationToken),
            ValidationException => HandleValidationException(exception, httpContext, cancellationToken),
            _ => HandleGeneralException(exception, httpContext, cancellationToken)
        };

        return await task;
    }

    private async Task<bool> HandleValidationException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception, "Validation error: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleInvalidOperationException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Invalid operation error: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Operation",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleNotFoundException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception, "Resource not found: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource Not Found",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleArgumentException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception, "Invalid argument: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Argument",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleArgumentNullException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Argument null exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleUnauthorizedException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception, "Unauthorized access attempt: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "You are not authorized to access this resource"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleDbUpdateException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Database update error: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Database Operation Failed",
            Detail = "The requested operation could not be completed due to a database conflict"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleConcurrencyException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Concurrency error: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency Conflict",
            Detail = "The resource was modified by another user. Please refresh and try again"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async Task<bool> HandleGeneralException(Exception exception, HttpContext httpContext, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}