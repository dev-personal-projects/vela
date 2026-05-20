namespace Vela.Api.Functions.Support;

public sealed record ApiErrorResponse(ApiError Error);

public sealed record ApiError(string Code, string Message, string RequestId);
