namespace Vela.Application.Interfaces;

public interface IApiTokenService
{
    string CreateAccessToken(Guid userId);
}

