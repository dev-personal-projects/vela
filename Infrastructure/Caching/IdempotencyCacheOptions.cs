namespace Vela.Infrastructure.Caching;

public sealed class IdempotencyCacheOptions
{
    public int TtlHours { get; init; } = 24;
}
