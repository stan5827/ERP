using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace ErpBackend.IntegrationTests;

public sealed class SupplierApSubmissionMigrationIntegrationTests
{
    private const string PreviousMigration = "20261027170100_AddSupplierCapaReviewAndAttachments";
    private const string SubmissionMigration = "20261027180000_AddSupplierApSubmissions";

    [PostgreSqlFact]
    public async Task Migration_AddsAndRemovesSupplierApSubmissionSchema()
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
            await using var createSchema = new NpgsqlCommand(
                $"CREATE SCHEMA \"{schema}\"",
                adminConnection);
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
            var upScript = migrator.GenerateScript(PreviousMigration, SubmissionMigration);
            await db.Database.ExecuteSqlRawAsync(upScript);

            Assert.Equal(5, await CountTablesAsync(
                db,
                "SupplierApSubmissions",
                "SupplierApSubmissionRevisions",
                "SupplierApSubmissionRevisionLines",
                "SupplierApSubmissionAttachments",
                "SupplierApSubmissionReviews"));
            Assert.Equal("date", await GetColumnTypeAsync(
                db,
                "SupplierApSubmissionRevisions",
                "SupplierInvoiceDate"));
            Assert.Equal("jsonb", await GetColumnTypeAsync(
                db,
                "SupplierApSubmissionReviews",
                "MatchSnapshot"));
            Assert.True(await UniqueIndexExistsAsync(
                db,
                "IX_SupplierApSubmissions_SupplierId_SupplierInvoiceNumberKey"));
            Assert.True(await UniqueIndexExistsAsync(
                db,
                "IX_SupplierApSubmissions_SupplierId_IdempotencyKey"));
            Assert.Equal(17, await CountForeignKeysAsync(db));
            Assert.Equal(SubmissionMigration, await GetLatestMigrationAsync(db));

            var downScript = migrator.GenerateScript(SubmissionMigration, PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(downScript);

            Assert.Equal(0, await CountTablesAsync(
                db,
                "SupplierApSubmissions",
                "SupplierApSubmissionRevisions",
                "SupplierApSubmissionRevisionLines",
                "SupplierApSubmissionAttachments",
                "SupplierApSubmissionReviews"));
            Assert.Equal(1, await CountTablesAsync(db, "AccountsPayables"));
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
            VALUES ('20261027170100_AddSupplierCapaReviewAndAttachments', '8.0.11');
            CREATE TABLE "AccountsPayables" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "SupplierUsers" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "Suppliers" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "PurchaseOrders" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "GoodsReceipts" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "GoodsReceiptItems" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "PurchaseOrderItems" ("Id" uuid NOT NULL PRIMARY KEY);
            CREATE TABLE "Users" ("Id" uuid NOT NULL PRIMARY KEY);
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

    private static async Task<string?> GetColumnTypeAsync(
        ErpGeneralDbContext db,
        string tableName,
        string columnName) =>
        await db.Database.SqlQuery<string>(
            $"""
            SELECT data_type AS "Value"
            FROM information_schema.columns
            WHERE table_schema = current_schema()
                AND table_name = {tableName}
                AND column_name = {columnName}
            """).SingleOrDefaultAsync();

    private static async Task<bool> UniqueIndexExistsAsync(
        ErpGeneralDbContext db,
        string indexName) =>
        await db.Database.SqlQuery<bool>(
            $"""
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = current_schema()
                    AND indexname = {indexName}
                    AND indexdef LIKE 'CREATE UNIQUE INDEX%'
            ) AS "Value"
            """).SingleAsync();

    private static async Task<int> CountForeignKeysAsync(ErpGeneralDbContext db) =>
        await db.Database.SqlQueryRaw<int>(
            """
            SELECT COUNT(*)::integer AS "Value"
            FROM information_schema.table_constraints
            WHERE constraint_schema = current_schema()
                AND constraint_type = 'FOREIGN KEY'
                AND table_name LIKE 'SupplierApSubmission%'
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
