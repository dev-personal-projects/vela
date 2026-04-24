using System.Net;
using System.Text.Encodings.Web;
using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;

namespace FinanceTracker.Api.GraphQL.ErrorHandling;

public sealed class GraphQlHttpResponseFormatter : DefaultHttpResponseFormatter
{
    public GraphQlHttpResponseFormatter()
        : base(indented: false, encoder: JavaScriptEncoder.Default, timeProvider: new SystemTimeProvider())
    {
    }

    protected override HttpStatusCode OnDetermineStatusCode(
        IOperationResult result,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        var statusCode = base.OnDetermineStatusCode(result, format, proposedStatusCode);

        if (result.Errors is null || result.Errors.Count == 0)
        {
            return statusCode;
        }

        // Prefer auth status codes when the request is otherwise well-formed.
        if (HasErrorCode(result.Errors, "AUTH_NOT_AUTHENTICATED"))
        {
            return HttpStatusCode.Unauthorized;
        }

        if (HasErrorCode(result.Errors, "AUTH_NOT_AUTHORIZED"))
        {
            return HttpStatusCode.Forbidden;
        }

        if (HasErrorCode(result.Errors, "REQUEST_TIMEOUT"))
        {
            return HttpStatusCode.GatewayTimeout;
        }

        // If execution failed due to server-side issues, return 5xx.
        if (HasErrorCode(result.Errors, "DB_CONNECTION_STRING_INVALID") ||
            HasErrorCode(result.Errors, "DB_CONNECTION_ERROR") ||
            HasErrorCode(result.Errors, "DB_ERROR") ||
            HasErrorCode(result.Errors, "UNEXPECTED"))
        {
            return HttpStatusCode.InternalServerError;
        }

        return statusCode;
    }

    private static bool HasErrorCode(IReadOnlyList<IError> errors, string code) =>
        errors.Any(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));

    private sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
