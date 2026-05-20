using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Vela.Application.Services;
using Vela.Domain.Common;
using UnitTests.Common;

namespace UnitTests.Auth;

public sealed class AuthServiceSignInTests
{
    private readonly Mock<ISupabaseAuthClient> _authClient = new();
    private readonly Mock<IApiTokenService> _apiTokenService = new();

    [Fact(DisplayName = "UT-AUTH-SIGNIN-001 returns API token and refresh token for valid credentials")]
    public async Task SignInAsync_ShouldReturnApiTokenAndRefreshToken_WhenCredentialsAreValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var supabaseAccessToken = TestJwtFactory.CreateAccessToken(userId);
        var ct = TestContext.Current.CancellationToken;

        _authClient
            .Setup(x => x.SignInAsync("user@example.com", "secret123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppResult<SignInPayload>.Ok(new SignInPayload(supabaseAccessToken, "refresh-123", "bearer")));

        _apiTokenService
            .Setup(x => x.CreateAccessToken(userId))
            .Returns("api-access-token");

        var result = await service.SignInAsync(new SignInInput(" User@Example.com ", "secret123"), ct);

        result.IsSuccess.Should().BeTrue("UT-AUTH-SIGNIN-001 should succeed for valid credentials");
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("api-access-token");
        result.Value.RefreshToken.Should().Be("refresh-123");
        result.Value.TokenType.Should().Be("bearer");

        _authClient.Verify(x => x.SignInAsync("user@example.com", "secret123", It.IsAny<CancellationToken>()), Times.Once);
        _apiTokenService.Verify(x => x.CreateAccessToken(userId), Times.Once);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNIN-002 rejects invalid email before hitting dependencies")]
    public async Task SignInAsync_ShouldFail_WhenEmailIsInvalid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.SignInAsync(new SignInInput("bad-email", "secret123"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("A valid email address is required.");

        _authClient.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiTokenService.Verify(x => x.CreateAccessToken(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNIN-005 rejects missing passwords before hitting dependencies")]
    public async Task SignInAsync_ShouldFail_WhenPasswordIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.SignInAsync(new SignInInput("user@example.com", "   "), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Password is required.");

        _authClient.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiTokenService.Verify(x => x.CreateAccessToken(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNIN-003 fails when upstream token payload does not contain a valid subject")]
    public async Task SignInAsync_ShouldFail_WhenUpstreamTokenPayloadIsInvalid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        _authClient
            .Setup(x => x.SignInAsync("user@example.com", "secret123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppResult<SignInPayload>.Ok(new SignInPayload("not-a-valid-jwt", "refresh-123", "bearer")));

        var result = await service.SignInAsync(new SignInInput("user@example.com", "secret123"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Authentication failed — invalid token payload.");

        _apiTokenService.Verify(x => x.CreateAccessToken(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNIN-004 returns upstream authentication errors without minting an API token")]
    public async Task SignInAsync_ShouldReturnUpstreamError_WhenProviderAuthenticationFails()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        _authClient
            .Setup(x => x.SignInAsync("user@example.com", "wrong-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppResult<SignInPayload>.Fail("Invalid login credentials."));

        var result = await service.SignInAsync(new SignInInput("user@example.com", "wrong-password"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid login credentials.");

        _apiTokenService.Verify(x => x.CreateAccessToken(It.IsAny<Guid>()), Times.Never);
    }

    private AuthService CreateService() => new(_authClient.Object, _apiTokenService.Object);
}
