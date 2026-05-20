using System.Text.Json;
using Vela.Application.Interfaces;
using Vela.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions.Support;

public static class FunctionIdempotency
{
    private const int _maxKeyLength = 255;

    public static async Task<IActionResult> RunAsync<T>(
        HttpRequestData req,
        Guid userId,
        IIdempotencyService idempotencyService,
        Func<Task<AppResult<T>>> operation,
        int successStatusCode = StatusCodes.Status200OK,
        CancellationToken ct = default)
    {
        var key = FunctionRequest.GetIdempotencyKey(req);
        if (key is null)
        {
            return await ExecuteAsync(req, operation, successStatusCode, null, ct);
        }

        if (key.Length > _maxKeyLength)
        {
            return FunctionHttpResults.Validation(req, $"Idempotency-Key must be {_maxKeyLength} characters or fewer.");
        }

        var scope = $"{userId:N}:{req.Method}:{req.Url.AbsolutePath}";
        var replay = await idempotencyService.GetAsync(scope, key, ct);
        if (replay is not null)
        {
            return new ApiContentResult(replay.Body, replay.StatusCode, FunctionRequest.GetRequestId(req));
        }

        return await ExecuteAsync(req, operation, successStatusCode, (scope, key, idempotencyService), ct);
    }

    private static async Task<IActionResult> ExecuteAsync<T>(
        HttpRequestData req,
        Func<Task<AppResult<T>>> operation,
        int successStatusCode,
        (string Scope, string Key, IIdempotencyService Service)? replayTarget,
        CancellationToken ct)
    {
        var result = await operation();
        if (result.IsFailure)
        {
            return FunctionHttpResults.FromResult(req, result);
        }

        var body = JsonSerializer.Serialize(result.Value, FunctionJson.Options);
        if (replayTarget is not null)
        {
            await replayTarget.Value.Service.StoreAsync(
                replayTarget.Value.Scope,
                replayTarget.Value.Key,
                new IdempotencyReplay(successStatusCode, body),
                ct);
        }

        return new ApiContentResult(body, successStatusCode, FunctionRequest.GetRequestId(req));
    }
}
