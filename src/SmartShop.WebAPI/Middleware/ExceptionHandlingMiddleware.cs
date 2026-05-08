using System.Net;
using System.Text.Json;
using FluentValidation;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Models;

namespace SmartShop.WebAPI.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, env.IsDevelopment());
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, bool isDevelopment)
    {
        context.Response.ContentType = "application/json";

        List<string> internalErrors = isDevelopment
            ? new[] { $"{exception.GetType().Name}: {exception.Message}", exception.InnerException?.Message ?? "" }
                .Where(s => !string.IsNullOrEmpty(s)).ToList()
            : new List<string> { "Đã xảy ra lỗi hệ thống." };

        var (statusCode, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                ve.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            NotFoundException nfe => (HttpStatusCode.NotFound, new List<string> { nfe.Message }),
            ConflictException ce => (HttpStatusCode.Conflict, new List<string> { ce.Message }),
            ConcurrencyException cce => (HttpStatusCode.Conflict, new List<string> { cce.Message }),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, new List<string> { ue.Message }),
            ServiceUnavailableException sue => (HttpStatusCode.ServiceUnavailable, new List<string> { sue.Message }),
            _ => (HttpStatusCode.InternalServerError, internalErrors)
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
