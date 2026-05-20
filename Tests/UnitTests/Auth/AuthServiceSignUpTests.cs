using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Vela.Application.Services;
using Vela.Domain.Common;

namespace UnitTests.Auth;

public sealed class AuthServiceSignUpTests
{
    private readonly Mock<ISupabaseAuthClient> _authClient = new();
    private readonly Mock<IApiTokenService> _apiTokenService = new();

    [Fact(DisplayName = "UT-AUTH-SIGNUP-001 normalizes email and delegates valid sign-up requests")]
    public async Task SignUpAsync_ShouldNormalizeEmailAndDelegate_WhenInputIsValid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        _authClient
            .Setup(x => x.SignUpAsync("person@example.com", "secret123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppResult<SignUpPayload>.Ok(new SignUpPayload("Check your email")));

        var result = await service.SignUpAsync(new SignUpInput(" Person@Example.com ", "secret123"), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Message.Should().Be("Check your email");

        _authClient.Verify(x => x.SignUpAsync("person@example.com", "secret123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNUP-002 rejects passwords shorter than eight characters")]
    public async Task SignUpAsync_ShouldFail_WhenPasswordIsTooShort()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.SignUpAsync(new SignUpInput("person@example.com", "short"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Password must be at least 8 characters.");

        _authClient.Verify(x => x.SignUpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-AUTH-SIGNUP-003 rejects invalid email before calling the auth provider")]
    public async Task SignUpAsync_ShouldFail_WhenEmailIsInvalid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.SignUpAsync(new SignUpInput("not-an-email", "secret123"), ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("A valid email address is required.");

        _authClient.Verify(x => x.SignUpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private AuthService CreateService() => new(_authClient.Object, _apiTokenService.Object);
}
