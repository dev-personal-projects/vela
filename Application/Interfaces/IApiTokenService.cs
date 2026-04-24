namespace FinanceTracker.Application.Interfaces;

public interface IApiTokenService
{
    string CreateAccessToken(Guid userId);
}

