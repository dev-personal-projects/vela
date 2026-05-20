namespace Vela.Api.Functions.Support;

public sealed record PagedResponse<T>(IReadOnlyList<T> Data, PaginationMeta Pagination);

public sealed record PaginationMeta(int Limit, int Offset, int Count, int Total, bool HasMore);

public sealed record PaginationOptions(int Limit, int Offset)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;
}
