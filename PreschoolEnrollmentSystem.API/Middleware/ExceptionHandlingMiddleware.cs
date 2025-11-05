using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly bool _isDevelopment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isDevelopment = environment.IsDevelopment();
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue to the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An unhandled exception occurred while processing request {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // Handle the exception and return appropriate response
                await HandleExceptionAsync(context, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Set response content type
            context.Response.ContentType = "application/json";

            // Determine status code and error details based on exception type
            var (statusCode, error, message, details) = GetErrorDetails(exception);

            context.Response.StatusCode = (int)statusCode;

            // Create error response
            var errorResponse = new ErrorResponse
            {
                Error = error,
                Message = message,
                Details = _isDevelopment ? details : null, // Only show details in development
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            };

            // Serialize and write response
            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _isDevelopment
            });

            await context.Response.WriteAsync(json);
        }
        private (HttpStatusCode statusCode, string error, string message, string? details) GetErrorDetails(Exception exception)
        {
            return exception switch
            {
                // 400 Bad Request - Invalid input or validation errors
                ArgumentNullException argNullEx => (
                    HttpStatusCode.BadRequest,
                    "BadRequest",
                    $"Required parameter '{argNullEx.ParamName}' is missing",
                    argNullEx.StackTrace
                ),

                ArgumentException argEx => (
                    HttpStatusCode.BadRequest,
                    "BadRequest",
                    argEx.Message,
                    argEx.StackTrace
                ),

                // 403 Forbidden - Authenticated but not authorized (more specific pattern first)
                UnauthorizedAccessException forbiddenEx when forbiddenEx.Message.Contains("permission") => (
                    HttpStatusCode.Forbidden,
                    "Forbidden",
                    "You don't have permission to access this resource",
                    forbiddenEx.StackTrace
                ),

                // 401 Unauthorized - Authentication required or failed
                UnauthorizedAccessException unauthEx => (
                    HttpStatusCode.Unauthorized,
                    "Unauthorized",
                    "Authentication required or credentials are invalid",
                    unauthEx.StackTrace
                ),

                // 404 Not Found - Resource doesn't exist
                KeyNotFoundException notFoundEx => (
                    HttpStatusCode.NotFound,
                    "NotFound",
                    notFoundEx.Message,
                    notFoundEx.StackTrace
                ),

                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("not found") => (
                    HttpStatusCode.NotFound,
                    "NotFound",
                    invalidOpEx.Message,
                    invalidOpEx.StackTrace
                ),

                // 409 Conflict - Resource already exists or conflict with current state
                InvalidOperationException conflictEx when conflictEx.Message.Contains("already exists") => (
                    HttpStatusCode.Conflict,
                    "Conflict",
                    conflictEx.Message,
                    conflictEx.StackTrace
                ),

                // 422 Unprocessable Entity - Request was well-formed but semantically incorrect
                InvalidOperationException unprocessableEx when unprocessableEx.Message.Contains("invalid") => (
                    HttpStatusCode.UnprocessableEntity,
                    "UnprocessableEntity",
                    unprocessableEx.Message,
                    unprocessableEx.StackTrace
                ),

                // 429 Too Many Requests - Rate limiting
                InvalidOperationException rateLimitEx when rateLimitEx.Message.Contains("rate limit") => (
                    HttpStatusCode.TooManyRequests,
                    "TooManyRequests",
                    "Too many requests. Please try again later.",
                    rateLimitEx.StackTrace
                ),

                // 500 Internal Server Error - Unexpected errors
                _ => (
                    HttpStatusCode.InternalServerError,
                    "InternalServerError",
                    "An unexpected error occurred. Please try again later.",
                    exception.Message + "\n" + exception.StackTrace
                )
            };
        }
        private class ErrorResponse
        {
            public string Error { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? Details { get; set; }
            public DateTime Timestamp { get; set; }
            public string Path { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public string TraceId { get; set; } = string.Empty;
        }
    }
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}