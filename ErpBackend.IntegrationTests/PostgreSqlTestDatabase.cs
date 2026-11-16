using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ErpBackend.IntegrationTests;

internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly string _baseConnectionString;
    private readonly string _schema;

    private PostgreSqlTestDatabase(string baseConnectionString, string schema)
    {
        _baseConnectionString = baseConnectionString;
        _schema = schema;
        ConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = schema
        }.ConnectionString;
        Options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public string ConnectionString { get; }
    public DbContextOptions<ErpGeneralDbContext> Options { get; }

    public static async Task<PostgreSqlTestDatabase> CreateAsync(string baseConnectionString)
    {
        var schema = $"erp_test_{Guid.NewGuid():N}";
        var database = new PostgreSqlTestDatabase(baseConnectionString, schema);

        await using (var connection = new NpgsqlConnection(baseConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            await using var db = new ErpGeneralDbContext(database.Options);
            await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
            return database;
        }
        catch
        {
            await database.DropSchemaAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync() => await DropSchemaAsync();

    private async Task DropSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE",
            connection);
        await command.ExecuteNonQueryAsync();
    }
}
