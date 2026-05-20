using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.WebApi.Infrastructure
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken ct)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            var (statusCode, title, detail) = exception switch
            {
                UnauthorizedException => (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    exception.Message),

                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    exception.Message),

                BusinessRuleBrokenException => (
                    StatusCodes.Status400BadRequest,
                    "Business Rule Violation",
                    exception.Message),

                ValidationException => (
                    StatusCodes.Status400BadRequest,
                    "Validation Error",
                    "One or more validation failures occurred."),

                DbUpdateConcurrencyException => (
                    StatusCodes.Status409Conflict,
                    "Concurrency Error",
                    "The record was modified by another user."),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Server Error",
                    "An unexpected error occurred.")
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var validationErrors = (exception as ValidationException)?.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var response = new
            {
                status = statusCode,
                title,
                detail,
                errors = validationErrors
            };

            await httpContext.Response.WriteAsJsonAsync(response, ct);
            return true;
        }
    }
}