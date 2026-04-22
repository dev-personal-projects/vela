namespace FinanceTracker.Application.DTOs.Auth;

public sealed record SignUpInput(string Email, string Password);

public sealed record SignInInput(string Email, string Password);

public sealed record SignUpPayload(string Message);

public sealed record SignInPayload(string AccessToken, string RefreshToken, string TokenType = "bearer");

