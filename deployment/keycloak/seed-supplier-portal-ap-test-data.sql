BEGIN;

DO $seed$
DECLARE
    supplier_id UUID;
    sku_id UUID;
    warehouse_id UUID;
    purchase_order_id UUID := 'e82c7812-b355-4f4f-9f2b-6e9a1f917ad1';
    purchase_order_item_id UUID := 'a4ec180f-819c-46ea-b89f-63847919171f';
    goods_receipt_id UUID := '0e2498db-eb42-4503-8875-c9cdd7f1b527';
    goods_receipt_item_id UUID := '5f789784-6415-46ce-b9f3-4f90f4c6309d';
BEGIN
    IF current_database() <> 'ErpGeneral' THEN
        RAISE EXCEPTION 'Seeder is restricted to the ErpGeneral database; current database is %', current_database();
    END IF;

    SELECT "Id"
    INTO supplier_id
    FROM public."Suppliers"
    WHERE "SupplierCode" = 'SUP-TEST-PORTAL'
      AND lower("Status") = 'active'
      AND lower("ApprovalStatus") = 'approved';
    IF supplier_id IS NULL THEN
        RAISE EXCEPTION 'Expected active, approved supplier SUP-TEST-PORTAL was not found';
    END IF;

    SELECT "Id"
    INTO sku_id
    FROM public."Skus"
    WHERE "SkuNumber" = 'FG-001'
      AND lower("Status") = 'active'
      AND "IsPurchasable" = TRUE;
    IF sku_id IS NULL THEN
        RAISE EXCEPTION 'Expected active, purchasable SKU FG-001 was not found';
    END IF;

    SELECT "Id"
    INTO warehouse_id
    FROM public."Warehouses"
    WHERE "WarehouseCode" = 'FW'
      AND "IsActive" = TRUE;
    IF warehouse_id IS NULL THEN
        RAISE EXCEPTION 'Expected active warehouse FW was not found';
    END IF;

    INSERT INTO public."PurchaseOrders" (
        "Id", "PoNumber", "SupplierId", "PoDate", "Currency", "ExchangeRate",
        "TaxType", "Subtotal", "TaxAmount", "TotalAmount", "BaseSubtotal",
        "BaseTaxAmount", "BaseTotalAmount", "Status", "IsLocked", "Version",
        "DeliveryWarehouseId", "Notes", "CreatedAt", "UpdatedAt"
    )
    VALUES (
        purchase_order_id, 'PO-PORTAL-TEST-2026-001', supplier_id, CURRENT_DATE,
        'TWD', 1, 'taxable', 250, 0, 250, 250, 0, 250, 'confirmed', FALSE, 1,
        warehouse_id, 'Synthetic WIP supplier-portal AP test record',
        NOW(), NOW()
    )
    ON CONFLICT ("PoNumber") DO UPDATE SET
        "SupplierId" = EXCLUDED."SupplierId",
        "PoDate" = EXCLUDED."PoDate",
        "Currency" = EXCLUDED."Currency",
        "ExchangeRate" = EXCLUDED."ExchangeRate",
        "TaxType" = EXCLUDED."TaxType",
        "Subtotal" = EXCLUDED."Subtotal",
        "TaxAmount" = EXCLUDED."TaxAmount",
        "TotalAmount" = EXCLUDED."TotalAmount",
        "BaseSubtotal" = EXCLUDED."BaseSubtotal",
        "BaseTaxAmount" = EXCLUDED."BaseTaxAmount",
        "BaseTotalAmount" = EXCLUDED."BaseTotalAmount",
        "Status" = EXCLUDED."Status",
        "IsLocked" = EXCLUDED."IsLocked",
        "Version" = EXCLUDED."Version",
        "DeliveryWarehouseId" = EXCLUDED."DeliveryWarehouseId",
        "Notes" = EXCLUDED."Notes",
        "UpdatedAt" = NOW()
    RETURNING "Id" INTO purchase_order_id;

    INSERT INTO public."PurchaseOrderItems" (
        "Id", "PurchaseOrderId", "SkuId", "Quantity", "Unit", "UnitPrice",
        "Subtotal", "ReceivedQty", "Status", "ConversionRate", "InventoryQty",
        "InventoryUnitPrice", "InventorySubtotal"
    )
    VALUES (
        purchase_order_item_id, purchase_order_id, sku_id, 2, 'SET', 125,
        250, 2, 'received', 1, 2, 125, 250
    )
    ON CONFLICT ("Id") DO UPDATE SET
        "PurchaseOrderId" = EXCLUDED."PurchaseOrderId",
        "SkuId" = EXCLUDED."SkuId",
        "Quantity" = EXCLUDED."Quantity",
        "Unit" = EXCLUDED."Unit",
        "UnitPrice" = EXCLUDED."UnitPrice",
        "Subtotal" = EXCLUDED."Subtotal",
        "ReceivedQty" = EXCLUDED."ReceivedQty",
        "Status" = EXCLUDED."Status",
        "ConversionRate" = EXCLUDED."ConversionRate",
        "InventoryQty" = EXCLUDED."InventoryQty",
        "InventoryUnitPrice" = EXCLUDED."InventoryUnitPrice",
        "InventorySubtotal" = EXCLUDED."InventorySubtotal";

    INSERT INTO public."GoodsReceipts" (
        "Id", "GrNumber", "PurchaseOrderId", "SupplierId", "GrDate", "WarehouseId",
        "Status", "IqcStatus", "Currency", "ExchangeRate", "TotalTariff",
        "TotalFreight", "TotalLandedCost", "CreatedAt"
    )
    VALUES (
        goods_receipt_id, 'GR-PORTAL-TEST-2026-001', purchase_order_id, supplier_id,
        CURRENT_DATE, warehouse_id, 'received', 'passed', 'TWD', 1, 0, 0, 250, NOW()
    )
    ON CONFLICT ("GrNumber") DO UPDATE SET
        "PurchaseOrderId" = EXCLUDED."PurchaseOrderId",
        "SupplierId" = EXCLUDED."SupplierId",
        "GrDate" = EXCLUDED."GrDate",
        "WarehouseId" = EXCLUDED."WarehouseId",
        "Status" = EXCLUDED."Status",
        "IqcStatus" = EXCLUDED."IqcStatus",
        "Currency" = EXCLUDED."Currency",
        "ExchangeRate" = EXCLUDED."ExchangeRate",
        "TotalTariff" = EXCLUDED."TotalTariff",
        "TotalFreight" = EXCLUDED."TotalFreight",
        "TotalLandedCost" = EXCLUDED."TotalLandedCost"
    RETURNING "Id" INTO goods_receipt_id;

    INSERT INTO public."GoodsReceiptItems" (
        "Id", "GoodsReceiptId", "SkuId", "PoQty", "PoUnit", "ReceivedQty",
        "AcceptedQty", "RejectedQty", "InventoryUnit", "ConversionRate",
        "InventoryQty", "IqcResult", "Currency", "ExchangeRate",
        "ForeignCurrencyPrice", "LocalCurrencyPrice", "ForeignCurrencyAmount",
        "LocalCurrencyAmount", "TariffAmount", "FreightAmount", "TotalLandedCost"
    )
    VALUES (
        goods_receipt_item_id, goods_receipt_id, sku_id, 2, 'SET', 2, 2, 0,
        'SET', 1, 2, 'passed', 'TWD', 1, 125, 125, 250, 250, 0, 0, 250
    )
    ON CONFLICT ("Id") DO UPDATE SET
        "GoodsReceiptId" = EXCLUDED."GoodsReceiptId",
        "SkuId" = EXCLUDED."SkuId",
        "PoQty" = EXCLUDED."PoQty",
        "PoUnit" = EXCLUDED."PoUnit",
        "ReceivedQty" = EXCLUDED."ReceivedQty",
        "AcceptedQty" = EXCLUDED."AcceptedQty",
        "RejectedQty" = EXCLUDED."RejectedQty",
        "InventoryUnit" = EXCLUDED."InventoryUnit",
        "ConversionRate" = EXCLUDED."ConversionRate",
        "InventoryQty" = EXCLUDED."InventoryQty",
        "IqcResult" = EXCLUDED."IqcResult",
        "Currency" = EXCLUDED."Currency",
        "ExchangeRate" = EXCLUDED."ExchangeRate",
        "ForeignCurrencyPrice" = EXCLUDED."ForeignCurrencyPrice",
        "LocalCurrencyPrice" = EXCLUDED."LocalCurrencyPrice",
        "ForeignCurrencyAmount" = EXCLUDED."ForeignCurrencyAmount",
        "LocalCurrencyAmount" = EXCLUDED."LocalCurrencyAmount",
        "TariffAmount" = EXCLUDED."TariffAmount",
        "FreightAmount" = EXCLUDED."FreightAmount",
        "TotalLandedCost" = EXCLUDED."TotalLandedCost";
END
$seed$;

COMMIT;
