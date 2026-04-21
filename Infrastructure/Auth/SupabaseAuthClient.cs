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

        return await ParseResponseAsync(response, ct);
    }

    public async Task<AppResult<AuthPayload>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var url = "auth/v1/token?grant_type=password";
        var response = await httpClient.PostAsJsonAsync(url, new { email, password }, ct);

        return await ParseResponseAsync(response, ct);
    }

    private static async Task<AppResult<AuthPayload>> ParseResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>(cancellationToken: ct);
            return AppResult<AuthPayload>.Fail(error?.Message ?? "Authentication failed.");
        }

        var data = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(cancellationToken: ct);

        return data?.AccessToken is null
            ? AppResult<AuthPayload>.Fail("Authentication failed — no token returned.")
            : AppResult<AuthPayload>.Ok(new AuthPayload(data.AccessToken, data.TokenType ?? "bearer"));
    }

    private sealed class SupabaseAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }

    private sealed class SupabaseErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }
}
