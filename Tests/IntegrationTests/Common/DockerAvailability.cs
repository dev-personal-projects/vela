namespace IntegrationTests.Common;

public static class DockerAvailability
{
    public static bool ShouldSkipIntegrationTests =>
        !File.Exists("/var/run/docker.sock") &&
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));
}
