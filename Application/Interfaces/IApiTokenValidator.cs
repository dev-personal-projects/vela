using System.Security.Claims;
using Vela.Domain.Common;

namespace Vela.Application.Interfaces;

public interface IApiTokenValidator
{
    AppResult<ClaimsPrincipal> ValidateAccessToken(string? authorizationHeader);
}
