namespace Vela.Application.Interfaces;

public sealed record IdempotencyReplay(int StatusCode, string Body);

public interface IIdempotencyService
{
    Task<IdempotencyReplay?> GetAsync(string scope, string key, CancellationToken ct = default);
    Task StoreAsync(string scope, string key, IdempotencyReplay replay, CancellationToken ct = default);
}
