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
        Skip = DockerAvailability.SkipReason;
        SkipType = typeof(DockerAvailability);
        SkipWhen = nameof(DockerAvailability.ShouldSkipIntegrationTests);
    }
}
