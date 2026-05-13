namespace IntegrationTests.Common;

[CollectionDefinition("Postgres", DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>;
