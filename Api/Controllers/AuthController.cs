using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpInput input, CancellationToken ct)
    {
        var result = await authService.SignUpAsync(input, ct);

        // Email confirmation disabled — token returned immediately
        if (result.IsSuccess)
        {
            return StatusCode(201, result.Value);
        }

        // Email confirmation enabled — user created but needs to confirm
        if (result.Error!.Contains("confirm your email"))
        {
            return StatusCode(201, new { message = result.Error });
        }

        // Actual failure — validation or duplicate email
        return BadRequest(new { error = result.Error });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInInput input, CancellationToken ct)
    {
        var result = await authService.SignInAsync(input, ct);

        if (result.IsFailure)
        {
            var isValidationError = result.Error!.Contains("required") ||
                                    result.Error.Contains("valid email");

            return isValidationError
                ? BadRequest(new { error = result.Error })
                : Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
