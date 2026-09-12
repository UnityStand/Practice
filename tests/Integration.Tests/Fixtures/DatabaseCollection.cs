namespace Integration.Tests.Fixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
}
