using System.IdentityModel.Tokens.Jwt;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions.Support;

public sealed class FunctionAuth(IApiTokenValidator tokenValidator)
{
    public AppResult<Guid> GetUserId(HttpRequestData req)
    {
        var authorization = req.Headers.TryGetValues("Authorization", out var values)
            ? values.FirstOrDefault()
            : null;

        var tokenResult = tokenValidator.ValidateAccessToken(authorization);
        if (tokenResult.IsFailure)
        {
            return AppResult<Guid>.Fail(tokenResult.Error!);
        }

        var principal = tokenResult.Value!;
        var userIdRaw = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                        principal.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdRaw, out var userId)
            ? AppResult<Guid>.Ok(userId)
            : AppResult<Guid>.Fail("Access token does not contain a valid user id.");
    }
}
