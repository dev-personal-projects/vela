using Vela.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Vela.Api.Functions.Support;

public static class FunctionHttpResults
{
    public static IActionResult Validation(string? error) =>
        new BadRequestObjectResult(new { error = error ?? "Request is invalid." });

    public static IActionResult Unauthorized(string? error = null) =>
        new UnauthorizedObjectResult(new { error = error ?? "Unauthorized." });

    public static IActionResult NotFound(string? error = null) =>
        new NotFoundObjectResult(new { error = error ?? "Resource not found." });

    public static IActionResult FromResult<T>(AppResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return IsNotFound(result.Error)
            ? NotFound(result.Error)
            : Validation(result.Error);
    }

    public static IActionResult Deleted(AppResult result) =>
        result.IsSuccess
            ? new NoContentResult()
            : IsNotFound(result.Error)
                ? NotFound(result.Error)
                : Validation(result.Error);

    private static bool IsNotFound(string? error) =>
        error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
}
