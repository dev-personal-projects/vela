namespace IntegrationTests.Common;

public static class DockerAvailability
{
    public static bool ShouldSkipIntegrationTests => !HasDockerEndpoint;

    public static bool HasDockerEndpoint =>
        File.Exists("/var/run/docker.sock") ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));

    public static void EnsureSupportedDevcontainerDefaults()
    {
        if (!IsDevcontainer)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE")))
        {
            Environment.SetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE", "host.docker.internal");
        }
    }

    public static string Diagnostics =>
        "Docker/Testcontainers diagnostics: " +
        $"DEVCONTAINER={Environment.GetEnvironmentVariable("DEVCONTAINER") ?? "<unset>"}, " +
        $"DOCKER_HOST={Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "<unset>"}, " +
        $"TESTCONTAINERS_HOST_OVERRIDE={Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE") ?? "<unset>"}, " +
        $"docker.sock={(File.Exists("/var/run/docker.sock") ? "present" : "missing")}.";

    public static string SkipReason =>
        "Docker/Testcontainers access is not available in this environment. " +
        "Open the repository in the supported devcontainer or provide a Docker endpoint before running Postgres-backed integration tests.";

    private static bool IsDevcontainer =>
        string.Equals(
            Environment.GetEnvironmentVariable("DEVCONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
