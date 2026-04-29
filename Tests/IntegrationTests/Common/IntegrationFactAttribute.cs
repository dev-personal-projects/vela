namespace IntegrationTests.Common;

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
        : this(null, -1)
    {
    }

    public IntegrationFactAttribute(
        string? sourceFilePath,
        int sourceLineNumber)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = "Docker is not available, so Postgres-backed integration tests are skipped in this environment.";
        SkipType = typeof(DockerAvailability);
        SkipWhen = nameof(DockerAvailability.ShouldSkipIntegrationTests);
    }
}
