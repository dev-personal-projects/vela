namespace Vela.Api.Functions.Support;

public sealed record HealthResponse(string Status, string Service, DateTime Timestamp);
