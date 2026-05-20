using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace Vela.Api.Functions;

public sealed class AuthFunctions(IAuthService authService)
{
    [Function("SignUp")]
    [OpenApiOperation(operationId: "SignUp", tags: [ApiDocumentation.AuthTag], Summary = "Creates an account.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(SignUpInput), Required = true, Description = "Account credentials.")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, ApiDocumentation.Json, typeof(SignUpPayload), Description = "The account was created.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "The request body is invalid.")]
    public async Task<IActionResult> SignUpAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthSignUp)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var input = await FunctionJson.ReadJsonAsync<SignUpInput>(req, ct);
        if (input is null)
        {
            return FunctionHttpResults.Validation(req, "Request body is required.");
        }

        var result = await authService.SignUpAsync(input, ct);
        return result.IsSuccess
            ? FunctionHttpResults.Created(req, new { message = result.Value!.Message })
            : FunctionHttpResults.Validation(req, result.Error);
    }

    [Function("SignIn")]
    [OpenApiOperation(operationId: "SignIn", tags: [ApiDocumentation.AuthTag], Summary = "Signs in and returns JWT tokens.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(SignInInput), Required = true, Description = "Account credentials.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SignInPayload), Description = "JWT tokens for API access.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "The request body is invalid.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "The credentials are invalid.")]
    public async Task<IActionResult> SignInAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthSignIn)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var input = await FunctionJson.ReadJsonAsync<SignInInput>(req, ct);
        if (input is null)
        {
            return FunctionHttpResults.Validation(req, "Request body is required.");
        }

        var result = await authService.SignInAsync(input, ct);
        if (result.IsSuccess)
        {
            return FunctionHttpResults.Ok(req, result.Value);
        }

        var isValidationError = result.Error!.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                result.Error.Contains("valid email", StringComparison.OrdinalIgnoreCase);

        return isValidationError
            ? FunctionHttpResults.Validation(req, result.Error)
            : FunctionHttpResults.Unauthorized(req, result.Error);
    }
}
