namespace IntegrationTests.Common;

public static class DockerAvailability
{
    public static bool ShouldSkipIntegrationTests => !HasDockerEndpoint;

    public static bool HasDockerEndpoint =>
        File.Exists("/var/run/docker.sock") ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));

    public static string SkipReason =>
        "Docker/Testcontainers access is not available in this environment. " +
        "Open the repository in the supported devcontainer or provide a Docker endpoint before running Postgres-backed integration tests.";
}
