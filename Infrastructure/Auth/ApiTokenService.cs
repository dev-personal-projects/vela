using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Auth;

public sealed class ApiTokenService : IApiTokenService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TimeSpan _accessTokenLifetime;

    public ApiTokenService(IConfiguration configuration)
    {
        _signingKey = JwtSigningKeyFactory.Create(configuration["Auth:JwtSecret"]);

        var minutesRaw = configuration["Auth:AccessTokenMinutes"];
        _accessTokenLifetime = int.TryParse(minutesRaw, out var minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromMinutes(60);
    }

    public string CreateAccessToken(Guid userId)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(_accessTokenLifetime),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return _handler.WriteToken(token);
    }
}
