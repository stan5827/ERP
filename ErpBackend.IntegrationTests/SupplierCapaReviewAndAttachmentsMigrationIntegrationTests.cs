using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace ErpBackend.IntegrationTests;

public sealed class SupplierCapaReviewAndAttachmentsMigrationIntegrationTests
{
    private const string PreviousMigration = "20261027170000_AddSupplierCapaResponseHistory";
    private const string CapaWorkflowMigration = "20261027170100_AddSupplierCapaReviewAndAttachments";

    [PostgreSqlFact]
    public async Task Migration_AddsAndRemovesCapaReviewAndAttachmentSchema()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        var schema = $"erp_test_{Guid.NewGuid():N}";
        var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = schema
        }.ConnectionString;

        await using (var adminConnection = new NpgsqlConnection(baseConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", adminConnection);
            await createSchema.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            await using var db = new ErpGeneralDbContext(options);
            await CreatePreviousSchemaAsync(db);

            var migrator = db.GetService<IMigrator>();
            var upScript = migrator.GenerateScript(PreviousMigration, CapaWorkflowMigration);
            await db.Database.ExecuteSqlRawAsync(upScript);

            Assert.Equal(2, await CountTablesAsync(db, "SupplierCapaReviews", "SupplierCapaAttachments"));
            Assert.Equal(6, await CountColumnsAsync(
                db,
                "SupplierCapaAttachments",
                "StorageKey",
                "OriginalFileName",
                "ContentType",
                "SizeBytes",
                "Sha256",
                "ScanStatus"));
            Assert.Equal(CapaWorkflowMigration, await GetLatestMigrationAsync(db));

            var downScript = migrator.GenerateScript(CapaWorkflowMigration, PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(downScript);

            Assert.Equal(0, await CountTablesAsync(db, "SupplierCapaReviews", "SupplierCapaAttachments"));
            Assert.Equal(1, await CountTablesAsync(db, "SupplierCapas"));
            Assert.Equal(PreviousMigration, await GetLatestMigrationAsync(db));
        }
        finally
        {
            await using var adminConnection = new NpgsqlConnection(baseConnectionString);
            await adminConnection.OpenAsync();
            await using var dropSchema = new NpgsqlCommand(
                $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE",
                adminConnection);
            await dropSchema.ExecuteNonQueryAsync();
        }
    }

    private static async Task CreatePreviousSchemaAsync(ErpGeneralDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
                "ProductVersion" character varying(32) NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20261027170000_AddSupplierCapaResponseHistory', '8.0.11');
            CREATE TABLE "SupplierCapas" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "SupplierCapaResponseRevisions" ("Id" uuid NOT NULL PRIMARY KEY);
            """);
    }

    private static async Task<int> CountTablesAsync(
        ErpGeneralDbContext db,
        params string[] tableNames) =>
        await db.Database.SqlQuery<int>(
            $"""
            SELECT COUNT(*)::integer AS "Value"
            FROM information_schema.tables
            WHERE table_schema = current_schema()
                AND table_name = ANY({tableNames})
            """).SingleAsync();

    private static async Task<int> CountColumnsAsync(
        ErpGeneralDbContext db,
        string tableName,
        params string[] columnNames) =>
        await db.Database.SqlQuery<int>(
            $"""
            SELECT COUNT(*)::integer AS "Value"
            FROM information_schema.columns
            WHERE table_schema = current_schema()
                AND table_name = {tableName}
                AND column_name = ANY({columnNames})
            """).SingleAsync();

    private static async Task<string?> GetLatestMigrationAsync(ErpGeneralDbContext db) =>
        await db.Database.SqlQueryRaw<string>(
            """
            SELECT "MigrationId" AS "Value"
            FROM "__EFMigrationsHistory"
            ORDER BY "MigrationId" DESC
            LIMIT 1
            """).SingleOrDefaultAsync();
}
