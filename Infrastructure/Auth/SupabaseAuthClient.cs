using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Vela.Domain.Common;

namespace Vela.Infrastructure.Auth;

public sealed class SupabaseAuthClient(HttpClient httpClient) : ISupabaseAuthClient
{
    public async Task<AppResult<SignUpPayload>> SignUpAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("auth/v1/signup", new { email, password }, ct);

        return await ParseSignUpResponseAsync(response, ct);
    }

    public async Task<AppResult<SignInPayload>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var url = "auth/v1/token?grant_type=password";
        var response = await httpClient.PostAsJsonAsync(url, new { email, password }, ct);

        return await ParseResponseAsync(response, ct);
    }

    private static async Task<AppResult<SignUpPayload>> ParseSignUpResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorResult = await ParseErrorAsync(response, ct);
            return AppResult<SignUpPayload>.Fail(errorResult.Error!);
        }

        var rawContent = await response.Content.ReadAsStringAsync(ct);
        SupabaseAuthResponse? data = null;

        try
        {
            data = System.Text.Json.JsonSerializer.Deserialize<SupabaseAuthResponse>(rawContent);
        }
        catch { }

        // User created successfully — whether confirmation is on or off
        if (data?.AccessToken is not null || data?.Id is not null)
        {
            return AppResult<SignUpPayload>.Ok(new SignUpPayload("Account created successfully. Please sign in."));
        }

        return AppResult<SignUpPayload>.Fail("Sign up failed. Please try again.");
    }

    private static async Task<AppResult<SignInPayload>> ParseResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorResult = await ParseErrorAsync(response, ct);
            return AppResult<SignInPayload>.Fail(errorResult.Error!);
        }

        var data = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(cancellationToken: ct);

        return data?.AccessToken is null
            ? AppResult<SignInPayload>.Fail("Authentication failed — no token returned.")
            : AppResult<SignInPayload>.Ok(new SignInPayload(data.AccessToken, data.RefreshToken ?? string.Empty, data.TokenType ?? "bearer"));
    }

    private static async Task<AppResult<SignInPayload>> ParseErrorAsync(HttpResponseMessage response, CancellationToken ct)
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

        return AppResult<SignInPayload>.Fail(MapErrorMessage(response.StatusCode, rawMessage, rawDescription, rawCode));
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
