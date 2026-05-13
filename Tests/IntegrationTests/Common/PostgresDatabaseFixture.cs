using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Common;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    private Respawner? _respawner;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("The PostgreSQL test container has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        if (DockerAvailability.ShouldSkipIntegrationTests)
        {
            return;
        }

        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("finance_tracker_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureCreatedAsync();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"]
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize the PostgreSQL Testcontainers fixture. " +
                "Ensure Docker/Testcontainers access is available inside the supported devcontainer.",
                ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MapEnum<LoanType>("loan_type"))
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        if (DockerAvailability.ShouldSkipIntegrationTests)
        {
            return;
        }

        if (_respawner is null)
        {
            throw new InvalidOperationException(
                "The PostgreSQL test fixture was not initialized correctly. " +
                "The Respawn reset operation cannot run without a ready Testcontainers database.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
