using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Infrastructure.Auth;

public sealed class SupabaseAuthClient(HttpClient httpClient) : ISupabaseAuthClient
{
    public async Task<AppResult<AuthPayload>> SignUpAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("auth/v1/signup", new { email, password }, ct);

        return await ParseSignUpResponseAsync(response, ct);
    }

    public async Task<AppResult<AuthPayload>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var url = "auth/v1/token?grant_type=password";
        var response = await httpClient.PostAsJsonAsync(url, new { email, password }, ct);

        return await ParseResponseAsync(response, ct);
    }

    private static async Task<AppResult<AuthPayload>> ParseSignUpResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return await ParseErrorAsync(response, ct);
        }

        var rawContent = await response.Content.ReadAsStringAsync(ct);
        SupabaseAuthResponse? data = null;

        try
        {
            data = System.Text.Json.JsonSerializer.Deserialize<SupabaseAuthResponse>(rawContent);
        }
        catch { }

        // Email confirmation disabled — token returned immediately
        if (data?.AccessToken is not null)
        {
            return AppResult<AuthPayload>.Ok(new AuthPayload(data.AccessToken, data.RefreshToken ?? string.Empty, data.TokenType ?? "bearer"));
        }

        // Email confirmation enabled — user created but no token yet
        if (data?.Id is not null)
        {
            return AppResult<AuthPayload>.Fail("Account created. Please check your email to confirm your account before signing in.");
        }

        return AppResult<AuthPayload>.Fail("Sign up failed. Please try again.");
    }

    private static async Task<AppResult<AuthPayload>> ParseResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return await ParseErrorAsync(response, ct);
        }

        var data = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(cancellationToken: ct);

        return data?.AccessToken is null
            ? AppResult<AuthPayload>.Fail("Authentication failed — no token returned.")
            : AppResult<AuthPayload>.Ok(new AuthPayload(data.AccessToken, data.RefreshToken ?? string.Empty, data.TokenType ?? "bearer"));
    }

    private static async Task<AppResult<AuthPayload>> ParseErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var rawContent = await response.Content.ReadAsStringAsync(ct);
        SupabaseErrorResponse? error = null;

        try
        {
            error = System.Text.Json.JsonSerializer.Deserialize<SupabaseErrorResponse>(rawContent);
        }
        catch { }

        // Remove debug logging after confirming error shapes
        // Console.WriteLine($"[Supabase Error] Status: {(int)response.StatusCode} | Raw: {rawContent}");

        var rawMessage     = error?.Msg ?? error?.Message ?? string.Empty;
        var rawDescription = error?.ErrorDescription ?? string.Empty;
        var rawCode        = error?.ErrorCode ?? string.Empty;

        return AppResult<AuthPayload>.Fail(MapErrorMessage(response.StatusCode, rawMessage, rawDescription, rawCode));
    }

    private static string MapErrorMessage(
        System.Net.HttpStatusCode statusCode,
        string message,
        string description,
        string code)
    {
        // Match on error_code first — these are exact and reliable
        return code switch
        {
            "user_already_exists"          => "An account with this email already exists.",
            "invalid_credentials"          => "Incorrect email or password.",
            "email_not_confirmed"          => "Please confirm your email address before signing in.",
            "weak_password"                => "Password does not meet the minimum requirements.",
            "signups_disabled"             => "New registrations are currently disabled.",
            "over_request_rate_limit"      => "Too many attempts. Please wait a moment and try again.",
            "email_address_not_authorized" => "This email address is not authorised to sign up.",
            "user_not_found"               => "No account found with this email address.",
            _ => FallbackMessage(statusCode, message, description)
        };
    }

    private static string FallbackMessage(System.Net.HttpStatusCode statusCode, string message, string description)
    {
        var combined = $"{message} {description}".ToLowerInvariant();

        if (combined.Contains("invalid login") || combined.Contains("invalid credentials"))
        {
            return "Incorrect email or password.";
        }

        if (combined.Contains("user not found") || combined.Contains("no user"))
        {
            return "No account found with this email address.";
        }

        if (combined.Contains("already registered") || combined.Contains("already exists"))
        {
            return "An account with this email already exists.";
        }

        if (combined.Contains("email not confirmed"))
        {
            return "Please confirm your email address before signing in.";
        }
        return statusCode == System.Net.HttpStatusCode.Unauthorized
            ? "Incorrect email or password."
            : "Something went wrong. Please try again.";
    }

    private sealed class SupabaseAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }
    }

    private sealed class SupabaseErrorResponse
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }

        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; init; }

        [JsonPropertyName("code")]
        public int? Code { get; init; }
    }
}
