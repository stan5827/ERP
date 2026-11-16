using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace ErpBackend.IntegrationTests;

public sealed class SupplierPortalCollaborationMigrationIntegrationTests
{
    private const string PreviousMigration = "20261027121800_AddSupplierPortalInvitationLifecycle";
    private const string CollaborationMigration = "20261027130000_AddSupplierPortalCollaboration";

    [PostgreSqlFact]
    public async Task Migration_AddsAndRemovesOnlySupplierCollaborationSchema()
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
            var upScript = migrator.GenerateScript(PreviousMigration, CollaborationMigration);
            await db.Database.ExecuteSqlRawAsync(upScript);

            Assert.Equal(3, await CountTablesAsync(db,
                "RfqSupplierInvitations",
                "PurchaseOrderSupplierResponses",
                "PurchaseOrderSupplierResponseItems"));
            Assert.Equal(2, await CountColumnsAsync(db, "RfqSupplierResponses",
                "PromisedDeliveryDate",
                "SubmittedAt"));
            Assert.Equal(CollaborationMigration, await GetLatestMigrationAsync(db));

            var downScript = migrator.GenerateScript(CollaborationMigration, PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(downScript);

            Assert.Equal(0, await CountTablesAsync(db,
                "RfqSupplierInvitations",
                "PurchaseOrderSupplierResponses",
                "PurchaseOrderSupplierResponseItems"));
            Assert.Equal(0, await CountColumnsAsync(db, "RfqSupplierResponses",
                "PromisedDeliveryDate",
                "SubmittedAt"));
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
            VALUES ('20261027121800_AddSupplierPortalInvitationLifecycle', '8.0.11');
            CREATE TABLE "Rfqs" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "Suppliers" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "PurchaseOrders" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "PurchaseOrderItems" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "RfqSupplierResponses" ("Id" uuid NOT NULL PRIMARY KEY);
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
