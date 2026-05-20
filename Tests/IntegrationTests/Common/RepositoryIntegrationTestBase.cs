using Vela.Infrastructure.Data;

namespace IntegrationTests.Common;

public abstract class RepositoryIntegrationTestBase(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    protected PostgresDatabaseFixture Fixture { get; } = fixture;

    protected AppDbContext CreateDbContext() => Fixture.CreateDbContext();

    public ValueTask InitializeAsync()
    {
        if (DockerAvailability.ShouldSkipIntegrationTests)
        {
            return ValueTask.CompletedTask;
        }

        return new(Fixture.ResetAsync());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
