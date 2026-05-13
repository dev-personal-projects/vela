using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UnitTests.Common;

internal static class TestJwtFactory
{
    internal static string CreateAccessToken(Guid userId)
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
            ]);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
