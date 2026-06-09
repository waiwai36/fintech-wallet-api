using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using wallet.Exceptions;
using wallet.Models.Responses;

namespace wallet.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        public async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = MapException(exception);

            if (string.IsNullOrWhiteSpace(message))
            {
                message = statusCode switch
                {
                    StatusCodes.Status401Unauthorized => "Unauthorized access. Token is missing or invalid.",
                    StatusCodes.Status403Forbidden => "Forbidden. You do not have permission to access this resource.",
                    StatusCodes.Status400BadRequest => "Bad request  data.",
                    StatusCodes.Status404NotFound => "The requested resource was not found.",
                    _ => "An unexpected error occurred. Please try again later."
                };
            }
            
            else if (!_environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
            {
                message = "An unexpected error occurred. Please try again later.";
            }
            
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. Status: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                statusCode);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            
            var response = ApiResponse<object?>.Fail(message);
            await context.Response.WriteAsJsonAsync(response, JsonOptions);
        }

        public static (int StatusCode, string Message) MapException(Exception exception)
        {
            return exception switch
                {
                  AppException appException => (
                      appException.StatusCode,
                      string.IsNullOrWhiteSpace(appException.Message) || appException.Message.Contains("was thrown") ? null! : appException.Message
                  ),           
                KeyNotFoundException keyNotFoundException => (StatusCodes.Status404NotFound, keyNotFoundException.Message),
                InvalidOperationException invalidOperationException => (StatusCodes.Status400BadRequest, invalidOperationException.Message),
                UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, unauthorizedAccessException.Message),
                _ => (StatusCodes.Status500InternalServerError, exception.Message)
            };
        }
    }
}