START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierComplaints" ADD "AttachmentUrl" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierComplaints" ADD "ClosedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierComplaints" ADD "ClosureReason" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierComplaints" ADD "ResponsiblePerson" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierCapas" ADD "AttachmentUrl" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierCapas" ADD "ClosedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierCapas" ADD "ClosureReason" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "SupplierCapas" ADD "ResponsiblePerson" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "NonConformanceReports" ADD "AttachmentUrl" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "NonConformanceReports" ADD "ClosedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "NonConformanceReports" ADD "ClosureReason" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "NonConformanceReports" ADD "ResponsiblePerson" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE TABLE "QaReviewLogs" (
        "Id" uuid NOT NULL,
        "TargetType" character varying(30) NOT NULL,
        "TargetId" uuid NOT NULL,
        "ActionType" character varying(50) NOT NULL,
        "ActorName" character varying(100),
        "ResponsiblePerson" character varying(100),
        "Notes" character varying(1000),
        "AttachmentUrl" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" uuid,
        "Status" character varying(20) DEFAULT 'Active',
        CONSTRAINT "PK_QaReviewLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE INDEX "IX_AccountsPayables_GoodsReceiptId" ON "AccountsPayables" ("GoodsReceiptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE INDEX "IX_AccountsPayables_PurchaseOrderId" ON "AccountsPayables" ("PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE INDEX "IX_AccountsPayables_SupplierId" ON "AccountsPayables" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE INDEX "IX_AccountsPayableItems_SkuId" ON "AccountsPayableItems" ("SkuId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    CREATE INDEX "IX_QaReviewLogs_TargetType_TargetId_CreatedAt" ON "QaReviewLogs" ("TargetType", "TargetId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "AccountsPayableItems" ADD CONSTRAINT "FK_AccountsPayableItems_Skus_SkuId" FOREIGN KEY ("SkuId") REFERENCES "Skus" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "AccountsPayables" ADD CONSTRAINT "FK_AccountsPayables_GoodsReceipts_GoodsReceiptId" FOREIGN KEY ("GoodsReceiptId") REFERENCES "GoodsReceipts" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "AccountsPayables" ADD CONSTRAINT "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    ALTER TABLE "AccountsPayables" ADD CONSTRAINT "FK_AccountsPayables_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260510124654_QualityTraceabilityWave5') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260510124654_QualityTraceabilityWave5', '8.0.11');
    END IF;
END $EF$;
COMMIT;

