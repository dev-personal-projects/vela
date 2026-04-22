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

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInInput input, CancellationToken ct)
    {
        var result = await authService.SignInAsync(input, ct);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
