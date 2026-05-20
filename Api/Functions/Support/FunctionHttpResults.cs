using Vela.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions.Support;

public static class FunctionHttpResults
{
    public static IActionResult Ok(HttpRequestData req, object? value) =>
        new ApiObjectResult(value, StatusCodes.Status200OK, FunctionRequest.GetRequestId(req));

    public static IActionResult Created(HttpRequestData req, object? value) =>
        new ApiObjectResult(value, StatusCodes.Status201Created, FunctionRequest.GetRequestId(req));

    public static IActionResult Validation(HttpRequestData req, string? error) =>
        Error(req, StatusCodes.Status400BadRequest, "ValidationFailed", error ?? "Request is invalid.");

    public static IActionResult Unauthorized(HttpRequestData req, string? error = null) =>
        Error(req, StatusCodes.Status401Unauthorized, "Unauthorized", error ?? "Unauthorized.");

    public static IActionResult NotFound(HttpRequestData req, string? error = null) =>
        Error(req, StatusCodes.Status404NotFound, "ResourceNotFound", error ?? "Resource not found.");

    public static IActionResult Conflict(HttpRequestData req, string? error) =>
        Error(req, StatusCodes.Status409Conflict, "Conflict", error ?? "Request conflicts with current state.");

    public static IActionResult FromResult<T>(HttpRequestData req, AppResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(req, result.Value);
        }

        return IsNotFound(result.Error)
            ? NotFound(req, result.Error)
            : Validation(req, result.Error);
    }

    public static IActionResult Deleted(HttpRequestData req, AppResult result) =>
        result.IsSuccess
            ? new ApiStatusCodeResult(StatusCodes.Status204NoContent, FunctionRequest.GetRequestId(req))
            : IsNotFound(result.Error)
                ? NotFound(req, result.Error)
                : Validation(req, result.Error);

    public static IActionResult OkList<T>(HttpRequestData req, IReadOnlyList<T> items, PaginationOptions? pagination)
    {
        if (pagination is null)
        {
            return Ok(req, items);
        }

        var data = items
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToList();

        var meta = new PaginationMeta(
            pagination.Limit,
            pagination.Offset,
            data.Count,
            items.Count,
            pagination.Offset + data.Count < items.Count);

        return Ok(req, new PagedResponse<T>(data, meta));
    }

    private static bool IsNotFound(string? error) =>
        error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;

    private static IActionResult Error(HttpRequestData req, int statusCode, string code, string message)
    {
        var requestId = FunctionRequest.GetRequestId(req);
        return new ApiObjectResult(new ApiErrorResponse(new ApiError(code, message, requestId)), statusCode, requestId);
    }
}
