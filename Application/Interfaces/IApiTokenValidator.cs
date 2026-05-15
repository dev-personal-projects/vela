using System.Security.Claims;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface IApiTokenValidator
{
    AppResult<ClaimsPrincipal> ValidateAccessToken(string? authorizationHeader);
}
