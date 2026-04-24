using System.Diagnostics;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinanceTracker.Api.GraphQL.ErrorHandling;

public sealed class GraphQlErrorFilter(IHostEnvironment env, ILogger<GraphQlErrorFilter> logger) : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        var traceId = Activity.Current?.Id;

        var mapped = error.Exception switch
        {
            ArgumentException ex when LooksLikeConnectionStringIssue(ex) =>
                Map(error, "DB_CONNECTION_STRING_INVALID", "Database configuration error.", traceId),
            ArgumentException =>
                Map(error, "BAD_REQUEST", "Invalid request.", traceId),
            DbUpdateException => Map(error, "DB_ERROR", "Database error. Please try again.", traceId),
            PostgresException => Map(error, "DB_ERROR", "Database error. Please try again.", traceId),
            NpgsqlException => Map(error, "DB_CONNECTION_ERROR", "Database connection error. Please try again.", traceId),
            _ => Map(error, "UNEXPECTED", "Unexpected server error. Please try again.", traceId)
        };

        logger.LogError(
            error.Exception,
            "GraphQL error {Code}. TraceId={TraceId}",
            mapped.Code ?? "UNKNOWN",
            traceId ?? "n/a");

        // Even in development, avoid sending exception details to clients since they can
        // contain secrets (e.g., connection strings). Use server logs for full details.
        return env.IsDevelopment() ? mapped : mapped;
    }

    private static IError Map(IError error, string code, string message, string? traceId)
    {
        var mapped = error.WithMessage(message).WithCode(code);

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            mapped = mapped.SetExtension("traceId", traceId);
        }

        // These extensions are added when IncludeExceptionDetails is enabled.
        if (mapped.Extensions?.ContainsKey("message") == true)
        {
            mapped = mapped.RemoveExtension("message");
        }

        if (mapped.Extensions?.ContainsKey("stackTrace") == true)
        {
            mapped = mapped.RemoveExtension("stackTrace");
        }

        return mapped;
    }

    private static bool LooksLikeConnectionStringIssue(ArgumentException ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("connectionstring") ||
               msg.Contains("postgresql://") ||
               msg.Contains("postgres://") ||
               msg.Contains("sslmode");
    }
}
