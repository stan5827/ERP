BEGIN;

DO $iqc_quarantine$
DECLARE
    v_receipt "GoodsReceipts"%ROWTYPE;
    v_item "GoodsReceiptItems"%ROWTYPE;
    v_sku "Skus"%ROWTYPE;
    v_lot "InventoryLots"%ROWTYPE;
    v_report_id uuid;
    v_hold_bin_id uuid;
    v_hold_bin_active boolean;
    v_source_location text;
    v_source_balance numeric;
    v_hold_balance numeric;
    v_source_lot_balance numeric;
    v_hold_lot_balance numeric;
    v_report_transaction_count integer;
    v_valid_transaction_count integer;
    v_audit_count integer;
    v_now timestamptz := clock_timestamp();
BEGIN
    SELECT *
    INTO STRICT v_receipt
    FROM "GoodsReceipts"
    WHERE "GrNumber" = 'GR-598471611347-0002'
    FOR UPDATE;

    SELECT *
    INTO STRICT v_item
    FROM "GoodsReceiptItems"
    WHERE "GoodsReceiptId" = v_receipt."Id"
      AND "LotNumber" = 'APC-001-001'
      AND "SkuId" = (
          SELECT "Id" FROM "Skus" WHERE "SkuNumber" = 'FG-001-06')
    FOR UPDATE;

    SELECT *
    INTO STRICT v_sku
    FROM "Skus"
    WHERE "Id" = v_item."SkuId";

    IF v_receipt."Status" <> 'received'
       OR v_receipt."IqcStatus" <> 'pending'
       OR v_sku."IqcRequired" IS DISTINCT FROM TRUE
       OR v_item."ReceivedQty" <> 50
       OR v_item."InventoryQty" <> 50
       OR v_item."RejectedQty" <> 0
       OR v_item."IqcResult" <> 'pending' THEN
        RAISE EXCEPTION 'Historical receipt no longer matches the approved IQC quarantine preconditions';
    END IF;

    SELECT "Id", "IsActive"
    INTO v_hold_bin_id, v_hold_bin_active
    FROM "BinLocations"
    WHERE "WarehouseId" = v_receipt."WarehouseId"
      AND "LocationCode" = 'IQC-HOLD'
    FOR UPDATE;

    SELECT "Id"
    INTO v_report_id
    FROM "IqcReports"
    WHERE "GoodsReceiptItemId" = v_item."Id";

    IF FOUND THEN
        SELECT COUNT(*),
               COUNT(*) FILTER (
                   WHERE ("Type" = 'iqc_quarantine_transfer_out' AND "QtyChange" = -50)
                      OR ("Type" = 'iqc_quarantine_transfer_in' AND "QtyChange" = 50))
        INTO v_report_transaction_count, v_valid_transaction_count
        FROM "InventoryTransactions"
        WHERE "RelatedDocType" = 'IqcReport'
          AND "RelatedDocId" = v_report_id;

        SELECT COUNT(*)
        INTO v_audit_count
        FROM "AuditLogs"
        WHERE "Action" = 'IQC_HISTORICAL_QUARANTINE'
          AND "EntityId" = v_item."Id";

        IF v_hold_bin_id IS NOT NULL
           AND v_hold_bin_active
           AND v_item."BinLocationId" = v_hold_bin_id
           AND v_item."AcceptedQty" = 0
           AND v_item."RejectedQty" = 0
           AND v_item."IqcResult" = 'pending'
           AND v_receipt."IqcStatus" = 'pending'
           AND v_report_transaction_count = 2
           AND v_valid_transaction_count = 2
           AND v_audit_count > 0
           AND EXISTS (
               SELECT 1
               FROM "IqcReports" r
               JOIN "InventoryLots" l
                 ON l."SkuId" = r."SkuId"
                AND l."LotNumber" = r."LotNumber"
                AND l."WarehouseId" = v_receipt."WarehouseId"
                AND l."BinLocationId" = v_hold_bin_id
               WHERE r."Id" = v_report_id
                 AND r."Result" = 'pending'
                 AND r."ReceivedQty" = 50
                 AND r."LotNumber" = 'APC-001-001'
                 AND l."Qty" = 50
                 AND l."Status" = 'quarantine'
           ) THEN
            RAISE NOTICE 'Historical IQC quarantine is already complete; no changes made.';
            RETURN;
        END IF;

        RAISE EXCEPTION 'A linked IQC report exists, but the historical quarantine state is incomplete or inconsistent';
    END IF;

    IF v_hold_bin_id IS NOT NULL AND NOT v_hold_bin_active THEN
        RAISE EXCEPTION 'The FW02 IQC-HOLD bin exists but is inactive';
    END IF;

    IF v_item."AcceptedQty" <> 50
       OR v_item."BinLocationId" IS NULL THEN
        RAISE EXCEPTION 'Historical receipt accepted quantity or source bin differs from the approved baseline';
    END IF;

    SELECT "LocationCode"
    INTO STRICT v_source_location
    FROM "BinLocations"
    WHERE "Id" = v_item."BinLocationId"
      AND "WarehouseId" = v_receipt."WarehouseId";

    IF v_source_location <> 'C-01-R01-02-P03' THEN
        RAISE EXCEPTION 'Historical receipt source bin differs from the approved baseline: %', v_source_location;
    END IF;

    SELECT *
    INTO STRICT v_lot
    FROM "InventoryLots"
    WHERE "SkuId" = v_sku."Id"
      AND "LotNumber" = 'APC-001-001'
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_item."BinLocationId"
    FOR UPDATE;

    IF v_lot."Qty" <> 50 OR v_lot."Status" <> 'active' THEN
        RAISE EXCEPTION 'Historical inventory lot quantity or status differs from the approved baseline';
    END IF;

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_source_lot_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_item."BinLocationId"
      AND "LotNumber" = 'APC-001-001';

    IF v_source_lot_balance <> 50 THEN
        RAISE EXCEPTION 'Historical source-bin lot ledger must equal 50; found %', v_source_lot_balance;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "IqcReports"
        WHERE "ReportNumber" = 'IQC-GR-598471611347-0002-HIST'
    ) THEN
        RAISE EXCEPTION 'The historical IQC report number is already in use';
    END IF;

    IF v_hold_bin_id IS NULL THEN
        INSERT INTO "BinLocations" (
            "Id", "WarehouseId", "LocationCode", "Zone", "Rack", "IsActive")
        VALUES (
            gen_random_uuid(), v_receipt."WarehouseId", 'IQC-HOLD', 'QA', 'HOLD', TRUE)
        RETURNING "Id" INTO v_hold_bin_id;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "InventoryLots"
        WHERE "SkuId" = v_sku."Id"
          AND "LotNumber" = 'APC-001-001'
          AND "WarehouseId" = v_receipt."WarehouseId"
          AND "BinLocationId" = v_hold_bin_id
    ) THEN
        RAISE EXCEPTION 'The target IQC-HOLD bin already contains this lot';
    END IF;

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_source_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_item."BinLocationId";

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_hold_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_hold_bin_id;

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_hold_lot_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_hold_bin_id
      AND "LotNumber" = 'APC-001-001';

    IF v_hold_lot_balance <> 0 THEN
        RAISE EXCEPTION 'The target IQC-HOLD bin already has ledger quantity % for this lot', v_hold_lot_balance;
    END IF;

    INSERT INTO "IqcReports" (
        "Id", "ReportNumber", "GoodsReceiptId", "GoodsReceiptItemId", "SupplierId",
        "SkuId", "ReceivedQty", "ReleaseBinLocationId", "InspectionDate", "InspectorId",
        "SampleSize", "AcceptNumber", "RejectNumber", "Result", "LotNumber", "CreatedAt")
    VALUES (
        gen_random_uuid(), 'IQC-GR-598471611347-0002-HIST', v_receipt."Id", v_item."Id",
        v_receipt."SupplierId", v_sku."Id", 50, v_item."BinLocationId", v_now, NULL,
        0, 0, 0, 'pending', 'APC-001-001', v_now)
    RETURNING "Id" INTO v_report_id;

    UPDATE "GoodsReceiptItems"
    SET "BinLocationId" = v_hold_bin_id,
        "AcceptedQty" = 0,
        "IqcResult" = 'pending',
        "ModifiedAt" = v_now
    WHERE "Id" = v_item."Id";

    UPDATE "InventoryLots"
    SET "BinLocationId" = v_hold_bin_id,
        "Status" = 'quarantine',
        "ModifiedAt" = v_now
    WHERE "Id" = v_lot."Id";

    INSERT INTO "InventoryTransactions" (
        "Id", "TrxId", "SkuId", "Type", "QtyChange", "QtyBefore", "QtyAfter",
        "LotNumber", "RelatedDocType", "RelatedDocId", "WarehouseId", "BinLocationId",
        "Notes", "CreatedAt")
    VALUES
        (gen_random_uuid(), 'IQC-HOLD-598471611347-0002-OUT', v_sku."Id",
         'iqc_quarantine_transfer_out', -50, v_source_balance, v_source_balance - 50,
         'APC-001-001', 'IqcReport', v_report_id, v_receipt."WarehouseId",
         v_item."BinLocationId", '歷史 IQC 隔離移轉：GR-598471611347-0002', v_now),
        (gen_random_uuid(), 'IQC-HOLD-598471611347-0002-IN', v_sku."Id",
         'iqc_quarantine_transfer_in', 50, v_hold_balance, v_hold_balance + 50,
         'APC-001-001', 'IqcReport', v_report_id, v_receipt."WarehouseId",
         v_hold_bin_id, '歷史 IQC 隔離移轉：GR-598471611347-0002', v_now);

    INSERT INTO "AuditLogs" (
        "Id", "Action", "EntityType", "EntityId", "TableName", "Details", "CreatedAt")
    VALUES (
        gen_random_uuid(), 'IQC_HISTORICAL_QUARANTINE', 'GoodsReceiptItem', v_item."Id",
        'GoodsReceiptItems',
        jsonb_build_object(
            'receiptNumber', 'GR-598471611347-0002',
            'lotNumber', 'APC-001-001',
            'quantity', 50,
            'sourceBin', v_source_location,
            'targetBin', 'IQC-HOLD',
            'acceptedQtyBefore', 50,
            'acceptedQtyAfter', 0,
            'inspectionReportId', v_report_id,
            'inspectionResult', 'pending',
            'reason', 'Approved historical IQC quarantine; physical quantity unchanged'
        )::text,
        v_now);

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_source_lot_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_item."BinLocationId"
      AND "LotNumber" = 'APC-001-001';

    SELECT COALESCE(SUM("QtyChange"), 0)
    INTO v_hold_lot_balance
    FROM "InventoryTransactions"
    WHERE "SkuId" = v_sku."Id"
      AND "WarehouseId" = v_receipt."WarehouseId"
      AND "BinLocationId" = v_hold_bin_id
      AND "LotNumber" = 'APC-001-001';

    IF v_source_lot_balance <> 0 OR v_hold_lot_balance <> 50 THEN
        RAISE EXCEPTION 'Post-transfer lot ledger mismatch: source %, IQC-HOLD %',
            v_source_lot_balance, v_hold_lot_balance;
    END IF;

    RAISE NOTICE 'Historical IQC quarantine completed: 50 units moved to FW02 IQC-HOLD; report % remains pending.',
        'IQC-GR-598471611347-0002-HIST';
END
$iqc_quarantine$;

COMMIT;
