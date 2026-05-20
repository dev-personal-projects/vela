using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions;

public sealed class AuthFunctions(IAuthService authService)
{
    [Function("SignUp")]
    public async Task<IActionResult> SignUpAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthSignUp)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var input = await FunctionJson.ReadJsonAsync<SignUpInput>(req, ct);
        if (input is null)
        {
            return FunctionHttpResults.Validation("Request body is required.");
        }

        var result = await authService.SignUpAsync(input, ct);
        return result.IsSuccess
            ? new ObjectResult(new { message = result.Value!.Message }) { StatusCode = 201 }
            : FunctionHttpResults.Validation(result.Error);
    }

    [Function("SignIn")]
    public async Task<IActionResult> SignInAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthSignIn)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var input = await FunctionJson.ReadJsonAsync<SignInInput>(req, ct);
        if (input is null)
        {
            return FunctionHttpResults.Validation("Request body is required.");
        }

        var result = await authService.SignInAsync(input, ct);
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var isValidationError = result.Error!.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                result.Error.Contains("valid email", StringComparison.OrdinalIgnoreCase);

        return isValidationError
            ? FunctionHttpResults.Validation(result.Error)
            : FunctionHttpResults.Unauthorized(result.Error);
    }
}
