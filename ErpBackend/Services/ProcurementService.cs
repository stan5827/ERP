using Microsoft.EntityFrameworkCore;
using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

// ──────────────── Supplier ────────────────
public interface ISupplierService
{
    Task<(List<SupplierDto> Items, int TotalCount)> GetSuppliersAsync(string? searchTerm, int page, int pageSize);
    Task<SupplierDto?> GetSupplierAsync(Guid id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request);
    Task UpdateSupplierAsync(Guid id, CreateSupplierRequest request);
    Task DeleteSupplierAsync(Guid id);
    Task SubmitForApprovalAsync(Guid id);
    Task ApproveSupplierAsync(Guid id, SupplierApprovalRequest req);
    Task RejectSupplierAsync(Guid id, SupplierApprovalRequest req);
    Task DeactivateSupplierAsync(Guid id, SupplierApprovalRequest req);
}

// ──────────────── Purchase Order ────────────────
public interface IPurchaseOrderService
{
    Task<(List<PurchaseOrderDto> Items, int TotalCount)> GetPurchaseOrdersAsync(string? status, string? searchTerm, int page, int pageSize);
    Task<PurchaseOrderDto?> GetPurchaseOrderAsync(Guid id);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request);
    Task ConfirmPurchaseOrderAsync(string idOrNumber);
    Task<string> ReceivePurchaseOrderAsync(string idOrNumber, ReceivePurchaseOrderRequest? request);
    Task DeletePurchaseOrderAsync(Guid id);
}

// ──────────────── Purchase Requisition ────────────────
public interface IPurchaseRequisitionService
{
    Task<(List<PurchaseRequisitionDto> Items, int TotalCount)> GetPurchaseRequisitionsAsync(string? status, string? searchTerm, int page, int pageSize);
    Task<PurchaseRequisitionDto?> GetPurchaseRequisitionAsync(Guid id);
    Task<PurchaseRequisitionDto> CreatePurchaseRequisitionAsync(CreatePurchaseRequisitionRequest req);
    Task UpdatePurchaseRequisitionAsync(Guid id, UpdatePurchaseRequisitionRequest req);
    Task UpdateStatusAsync(Guid id, string status);
    Task ToggleLockAsync(Guid id);
    Task SubmitPRAsync(Guid id, ApprovalWorkflowService approvalService);
    Task ApprovePRAsync(Guid id, ApprovalActionRequest req, ApprovalWorkflowService approvalService);
    Task RejectPRAsync(Guid id, ApprovalActionRequest req);
    Task ReturnPRForEditAsync(Guid id, ApprovalActionRequest req);
    Task<ApprovalChainResult> GetApprovalChainAsync(Guid id, ApprovalWorkflowService approvalService);
    Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(Guid id);
    Task<string> ConvertToPOAsync(Guid id, ConvertToPoRequest req);
}

public interface IGoodsReceiptService
{
    Task<(List<GoodsReceiptDto> Items, int TotalCount)> GetGoodsReceiptsAsync(int page, int pageSize);
}

// ──────────────── Procurement Service ────────────────
// 合成介面：統一 Procurement 模組對外契約
public interface IProcurementService : ISupplierService, IPurchaseOrderService, IPurchaseRequisitionService, IGoodsReceiptService
{
}

public class ProcurementService : IProcurementService
{
    private readonly ErpGeneralDbContext _db;

    public ProcurementService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<(List<GoodsReceiptDto> Items, int TotalCount)> GetGoodsReceiptsAsync(int page, int pageSize)
    {
        var query = _db.GoodsReceipts.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(receipt => receipt.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(receipt => new GoodsReceiptDto
            {
                Id = receipt.Id,
                GrNumber = receipt.GrNumber,
                PurchaseOrderId = receipt.PurchaseOrderId,
                PurchaseOrderNumber = receipt.PurchaseOrder == null ? null : receipt.PurchaseOrder.PoNumber,
                CreatedAt = receipt.CreatedAt,
                Status = receipt.Status,
                IqcStatus = receipt.IqcStatus
            })
            .ToListAsync();

        return (items, totalCount);
    }

    // ═══════════════════════════════════════════════
    //  SUPPLIER
    // ═══════════════════════════════════════════════

    public async Task<(List<SupplierDto> Items, int TotalCount)> GetSuppliersAsync(string? searchTerm, int page, int pageSize)
    {
        var query = _db.Suppliers.AsQueryable();
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(s => s.SupplierName.Contains(searchTerm) || s.SupplierCode.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.SupplierCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                SupplierCode = s.SupplierCode,
                SupplierName = s.SupplierName,
                TaxId = s.TaxId,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                InvoiceAddress = s.InvoiceAddress,
                DeliveryAddress = s.DeliveryAddress,
                PaymentTerms = s.PaymentTerms,
                Status = s.Status,
                Rating = s.Rating,
                Currency = s.Currency ?? "TWD",
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<SupplierDto?> GetSupplierAsync(Guid id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return null;
        return new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName,
            TaxId = s.TaxId,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            InvoiceAddress = s.InvoiceAddress,
            DeliveryAddress = s.DeliveryAddress,
            PaymentTerms = s.PaymentTerms,
            Status = s.Status,
            Rating = s.Rating,
            Currency = s.Currency,
            CreatedAt = s.CreatedAt
        };
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request)
    {
        if (await _db.Suppliers.AnyAsync(s => s.SupplierCode == request.SupplierCode))
            throw new InvalidOperationException("供應商編號已存在");

        var supplier = new Supplier
        {
            SupplierCode = request.SupplierCode,
            SupplierName = request.SupplierName,
            TaxId = request.TaxId,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            InvoiceAddress = request.InvoiceAddress,
            DeliveryAddress = request.DeliveryAddress,
            PaymentTerms = request.PaymentTerms,
            Currency = request.Currency ?? "TWD",
            Status = "ACTIVE"
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            TaxId = supplier.TaxId,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            InvoiceAddress = supplier.InvoiceAddress,
            DeliveryAddress = supplier.DeliveryAddress,
            PaymentTerms = supplier.PaymentTerms,
            Rating = supplier.Rating,
            Status = supplier.Status,
            Currency = supplier.Currency,
            CreatedAt = supplier.CreatedAt
        };
    }

    public async Task UpdateSupplierAsync(Guid id, CreateSupplierRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");

        supplier.SupplierName = request.SupplierName;
        supplier.TaxId = request.TaxId;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.InvoiceAddress = request.InvoiceAddress;
        supplier.DeliveryAddress = request.DeliveryAddress;
        supplier.PaymentTerms = request.PaymentTerms;
        supplier.Currency = request.Currency ?? "TWD";
        supplier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteSupplierAsync(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
    }

    public async Task SubmitForApprovalAsync(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");
        if (supplier.ApprovalStatus == "APPROVED")
            throw new InvalidOperationException("供應商已通過審批");
        supplier.ApprovalStatus = "UNDER_REVIEW";
        await _db.SaveChangesAsync();
    }

    public async Task ApproveSupplierAsync(Guid id, SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");
        if (supplier.ApprovalStatus == "APPROVED")
            throw new InvalidOperationException("供應商已通過審批");
        supplier.ApprovalStatus = "APPROVED";
        supplier.Status = "ACTIVE";
        supplier.ApprovedBy = req.EmployeeId;
        supplier.ApprovedAt = DateTime.UtcNow;
        supplier.ApprovalNotes = req.Comments;
        await _db.SaveChangesAsync();
    }

    public async Task RejectSupplierAsync(Guid id, SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");
        supplier.ApprovalStatus = "REJECTED";
        supplier.ApprovalNotes = req.Comments;
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateSupplierAsync(Guid id, SupplierApprovalRequest req)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new KeyNotFoundException("供應商不存在");
        supplier.Status = "INACTIVE";
        supplier.ApprovalNotes = req.Comments;
        await _db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════
    //  PURCHASE ORDER
    // ═══════════════════════════════════════════════

    public async Task<(List<PurchaseOrderDto> Items, int TotalCount)> GetPurchaseOrdersAsync(string? status, string? searchTerm, int page, int pageSize)
    {
        var query = _db.PurchaseOrders.Include(po => po.Supplier).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(po => po.Status == status);
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(po => po.PoNumber.Contains(searchTerm) || po.Supplier.SupplierName.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(po => po.PoDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderDto
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                SupplierId = po.SupplierId,
                SupplierCode = po.Supplier.SupplierCode,
                SupplierName = po.Supplier.SupplierName,
                PoDate = po.PoDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Subtotal = po.Subtotal,
                TaxAmount = po.TaxAmount,
                TotalAmount = po.TotalAmount,
                ReceivedAmount = po.ReceivedAmount ?? 0m,
                Status = po.Status,
                Currency = po.Currency,
                CreatedAt = po.CreatedAt,
                Items = po.Items.Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    SkuId = i.SkuId,
                    SkuNumber = i.Sku != null ? i.Sku.SkuNumber : null,
                    SkuName = i.Sku != null ? i.Sku.SkuName : null,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal,
                    ReceivedQty = i.ReceivedQty,
                    Status = i.Status,
                    LotControlled = i.Sku != null && i.Sku.LotControlled
                }).ToList()
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderAsync(Guid id)
    {
        var po = await _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (po == null) return null;

        return new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId,
            SupplierCode = po.Supplier?.SupplierCode,
            SupplierName = po.Supplier?.SupplierName,
            PoDate = po.PoDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Subtotal = po.Subtotal,
            TaxAmount = po.TaxAmount,
            TotalAmount = po.TotalAmount,
            ReceivedAmount = po.ReceivedAmount ?? 0m,
            Status = po.Status,
            Currency = po.Currency,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ReceivedQty = i.ReceivedQty,
                Status = i.Status,
                LotControlled = i.Sku?.LotControlled ?? false
            }).ToList()
        };
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(request.SupplierId)
            ?? throw new KeyNotFoundException("找不到供應商");

        var poNumber = DocumentNumberGenerator.GeneratePurchaseOrder();
        var subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = subtotal * 0.05m;
        var totalAmount = subtotal + taxAmount;

        var po = new PurchaseOrder
        {
            PoNumber = poNumber,
            SupplierId = request.SupplierId,
            PoDate = DateTime.UtcNow,
            Currency = supplier.Currency,
            Status = "draft",
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            po.Items.Add(new PurchaseOrderItem
            {
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Quantity * item.UnitPrice,
                Status = "pending"
            });
        }

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        // Reload for supplier name
        await _db.Entry(po).Reference(p => p.Supplier).LoadAsync();
        await _db.Entry(po).Collection(p => p.Items).LoadAsync();

        return new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName,
            PoDate = po.PoDate,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ReceivedQty = i.ReceivedQty,
                LotControlled = i.Sku?.LotControlled ?? false
            }).ToList()
        };
    }

    public async Task ConfirmPurchaseOrderAsync(string idOrNumber)
    {
        var po = await FindPurchaseOrderAsync(idOrNumber)
            ?? throw new KeyNotFoundException("找不到採購單");

        if (po.Status != "draft" && po.Status != "pending")
            throw new InvalidOperationException("採購單狀態必須為 draft 或 pending 才能確認");

        po.Status = "confirmed";
        await _db.SaveChangesAsync();
    }

    public async Task<string> ReceivePurchaseOrderAsync(string idOrNumber, ReceivePurchaseOrderRequest? request)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;

        var po = await FindPurchaseOrderWithItemsAsync(idOrNumber)
            ?? throw new KeyNotFoundException("找不到採購單");
        po = await LockPurchaseOrderAsync(po.Id)
            ?? throw new KeyNotFoundException("找不到採購單");
        await _db.Entry(po).ReloadAsync();
        foreach (var purchaseOrderItem in po.Items)
            await _db.Entry(purchaseOrderItem).ReloadAsync();

        if (po.Status is not ("confirmed" or "partial_received"))
            throw new InvalidOperationException("採購單必須為 confirmed 或 partial_received 才能收貨");
        if (po.IsLocked)
            throw new InvalidOperationException("採購單已鎖定，無法收貨");

        var requestedItems = request?.Items is not null
            ? request.Items
            : request is not null && request.ItemId != Guid.Empty
                ? new List<ReceivePurchaseOrderItemRequestDto>
                {
                    new()
                    {
                        PurchaseOrderItemId = request.ItemId,
                        Quantity = request.Quantity
                    }
                }
                : po.Items
                    .Where(item => item.Quantity > item.ReceivedQty)
                    .Select(item => new ReceivePurchaseOrderItemRequestDto
                    {
                        PurchaseOrderItemId = item.Id,
                        Quantity = item.Quantity - item.ReceivedQty
                    })
                    .ToList();

        if (requestedItems.Count == 0)
            throw new InvalidOperationException("沒有尚待收貨的採購明細");
        if (requestedItems.Any(item => item.PurchaseOrderItemId == Guid.Empty || item.Quantity <= 0) ||
            requestedItems.Select(item => item.PurchaseOrderItemId).Distinct().Count() != requestedItems.Count)
            throw new InvalidOperationException("收貨明細必須指定不同的採購明細及正數數量");

        var poItemsById = po.Items.ToDictionary(item => item.Id);
        var selectedItems = new List<(PurchaseOrderItem PurchaseOrderItem, ReceivePurchaseOrderItemRequestDto Request)>();
        foreach (var requestedItem in requestedItems)
        {
            if (!poItemsById.TryGetValue(requestedItem.PurchaseOrderItemId, out var purchaseOrderItem))
                throw new InvalidOperationException("收貨明細不屬於此採購單");

            var remainingQuantity = purchaseOrderItem.Quantity - purchaseOrderItem.ReceivedQty;
            if (requestedItem.Quantity > remainingQuantity)
                throw new InvalidOperationException("收貨數量不可超過採購單未收數量");
            if (purchaseOrderItem.Sku?.LotControlled == true &&
                string.IsNullOrWhiteSpace(requestedItem.LotNumber))
                throw new InvalidOperationException($"物料 {purchaseOrderItem.Sku.SkuNumber} 需批號，收貨時必須填寫批號");

            selectedItems.Add((purchaseOrderItem, requestedItem));
        }

        Asn? linkedAsn = null;
        var previousAsnReceiptsBySkuAndLot = new Dictionary<(Guid SkuId, string? LotNumber), decimal>();
        if (request?.AsnId is Guid asnId)
        {
            linkedAsn = await _db.Asns
                .Include(asn => asn.Items)
                .SingleOrDefaultAsync(asn =>
                    asn.Id == asnId
                    && asn.PurchaseOrderId == po.Id
                    && asn.SupplierId == po.SupplierId);
            if (linkedAsn is null)
                throw new InvalidOperationException("出貨通知不存在或不屬於此採購單供應商");
            if (linkedAsn.Status is not ("submitted" or "partially_received"))
                throw new InvalidOperationException("此出貨通知目前不可收貨");

            previousAsnReceiptsBySkuAndLot = await _db.GoodsReceiptItems
                .Where(item => item.GoodsReceipt.AsnId == linkedAsn.Id)
                .GroupBy(item => new { item.SkuId, item.LotNumber })
                .Select(group => new
                {
                    group.Key.SkuId,
                    group.Key.LotNumber,
                    ReceivedQuantity = group.Sum(item => item.ReceivedQty)
                })
                .ToDictionaryAsync(
                    item => (item.SkuId, item.LotNumber),
                    item => item.ReceivedQuantity);

            var requestedBySkuAndLot = selectedItems
                .GroupBy(item => (
                    item.PurchaseOrderItem.SkuId,
                    LotNumber: NormalizeLotNumber(item.Request.LotNumber)))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Request.Quantity));
            var shippedBySkuAndLot = linkedAsn.Items
                .GroupBy(item => (item.SkuId, LotNumber: NormalizeLotNumber(item.LotNumber)))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.ShippedQty));
            foreach (var (skuAndLot, requestedQuantity) in requestedBySkuAndLot)
            {
                var previouslyReceived = previousAsnReceiptsBySkuAndLot.GetValueOrDefault(skuAndLot);
                var shippedQuantity = shippedBySkuAndLot.GetValueOrDefault(skuAndLot);
                if (requestedQuantity > shippedQuantity - previouslyReceived)
                    throw new InvalidOperationException("收貨數量不可超過此出貨通知尚未收貨的數量");
            }
        }

        Warehouse? warehouse = null;
        if (request?.WarehouseId is Guid requestedWarehouseId)
        {
            warehouse = await _db.Warehouses.FirstOrDefaultAsync(w =>
                w.Id == requestedWarehouseId && w.IsActive);
        }
        else if (!string.IsNullOrWhiteSpace(request?.WarehouseCode))
        {
            warehouse = await _db.Warehouses.FirstOrDefaultAsync(w =>
                w.WarehouseCode == request.WarehouseCode && w.IsActive);
        }
        else
        {
            warehouse = await _db.Warehouses.FirstOrDefaultAsync(w =>
                w.Type == "raw_material" && w.IsActive);
        }

        if (warehouse == null)
            throw new InvalidOperationException("找不到有效的入庫倉庫，請重新選擇倉庫");

        if (request?.BinLocationId is not Guid binLocationId)
            throw new InvalidOperationException("請選擇入庫儲位");

        var binLocation = await _db.BinLocations.FirstOrDefaultAsync(location =>
            location.Id == binLocationId &&
            location.WarehouseId == warehouse.Id &&
            location.IsActive);

        if (binLocation == null)
            throw new InvalidOperationException("所選儲位不存在、未啟用或不屬於此倉庫");

        var lockedSkus = new Dictionary<Guid, Sku>();
        foreach (var skuId in selectedItems.Select(item => item.PurchaseOrderItem.SkuId).Distinct().OrderBy(id => id))
        {
            lockedSkus[skuId] = await LockSkuAsync(skuId)
                ?? throw new InvalidOperationException("採購明細物料不存在");
        }

        var grNumber = DocumentNumberGenerator.Generate("GR");
        var gr = new GoodsReceipt
        {
            GrNumber = grNumber,
            PurchaseOrderId = po.Id,
            AsnId = linkedAsn?.Id,
            SupplierId = po.SupplierId,
            WarehouseId = warehouse.Id,
            GrDate = DateTime.UtcNow,
            Status = "received",
            CreatedAt = DateTime.UtcNow
        };

        var hasIqcRequiredItems = false;
        var balances = new Dictionary<(Guid SkuId, Guid? BinLocationId), decimal>();
        var lots = new Dictionary<(Guid SkuId, string LotNumber, Guid WarehouseId, Guid? BinLocationId), InventoryLot>();
        foreach (var (poItem, requestedItem) in selectedItems)
        {
            var item = lockedSkus[poItem.SkuId];
            var quantity = requestedItem.Quantity;
            var lotNumber = string.IsNullOrWhiteSpace(requestedItem.LotNumber)
                ? null
                : requestedItem.LotNumber.Trim();
            var asnItem = linkedAsn?.Items.FirstOrDefault(asnLine =>
                asnLine.SkuId == item.Id
                && NormalizeLotNumber(asnLine.LotNumber) == lotNumber);
            var localCurrencyPrice = poItem.UnitPrice * po.ExchangeRate;
            var requiresIqc = item.IqcRequired;
            var inventoryBin = requiresIqc
                ? await IqcQuarantineBinHelper.GetOrCreateAsync(_db, warehouse.Id)
                : binLocation;
            hasIqcRequiredItems |= requiresIqc;

            var receiptItem = new GoodsReceiptItem
            {
                SkuId = item.Id,
                PoQty = poItem.Quantity,
                PoUnit = poItem.Unit,
                ReceivedQty = quantity,
                AcceptedQty = requiresIqc ? 0 : quantity,
                InventoryQty = quantity,
                InventoryUnit = poItem.InventoryUnit ?? poItem.Unit,
                LotNumber = lotNumber,
                ManufactureDate = asnItem?.ManufactureDate,
                ExpiryDate = asnItem?.ExpiryDate,
                BinLocationId = inventoryBin.Id,
                IqcResult = requiresIqc ? "pending" : "not_required",
                Currency = po.Currency,
                ExchangeRate = po.ExchangeRate,
                ForeignCurrencyPrice = poItem.UnitPrice,
                LocalCurrencyPrice = localCurrencyPrice,
                ForeignCurrencyAmount = quantity * poItem.UnitPrice,
                LocalCurrencyAmount = quantity * localCurrencyPrice
            };
            gr.Items.Add(receiptItem);

            if (requiresIqc)
            {
                _db.IqcReports.Add(new ErpBackend.Models.QA.IqcReport
                {
                    ReportNumber = DocumentNumberGenerator.Generate("IQC"),
                    GoodsReceiptId = gr.Id,
                    GoodsReceiptItemId = receiptItem.Id,
                    SupplierId = gr.SupplierId,
                    SkuId = item.Id,
                    ReceivedQty = quantity,
                    ReleaseBinLocationId = binLocation.Id,
                    InspectionDate = DateTime.UtcNow,
                    InspectorId = null,
                    SampleSize = 0,
                    AcceptNumber = 0,
                    RejectNumber = 0,
                    Result = "pending",
                    LotNumber = lotNumber
                });
            }

            var balanceKey = (item.Id, (Guid?)inventoryBin.Id);
            if (!balances.TryGetValue(balanceKey, out var quantityBefore))
            {
                quantityBefore = await _db.InventoryTransactions
                    .Where(inventoryTransaction =>
                        inventoryTransaction.SkuId == item.Id &&
                        inventoryTransaction.WarehouseId == warehouse.Id &&
                        inventoryTransaction.BinLocationId == inventoryBin.Id)
                    .SumAsync(inventoryTransaction => (decimal?)inventoryTransaction.QtyChange) ?? 0m;
            }

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = $"GR-{Guid.NewGuid():N}"[..30],
                SkuId = item.Id,
                Type = "purchase_receipt",
                QtyChange = quantity,
                QtyBefore = quantityBefore,
                QtyAfter = quantityBefore + quantity,
                LotNumber = lotNumber,
                RelatedDocType = "GoodsReceipt",
                RelatedDocId = gr.Id,
                WarehouseId = warehouse.Id,
                BinLocationId = inventoryBin.Id,
                Notes = $"採購收貨 {grNumber}",
                UnitCost = localCurrencyPrice,
                TotalCost = quantity * localCurrencyPrice,
                CreatedAt = DateTime.UtcNow
            });

            item.Qty += quantity;
            poItem.ReceivedQty += quantity;
            poItem.Status = poItem.ReceivedQty >= poItem.Quantity ? "received" : "partial_received";
            po.ReceivedAmount = (po.ReceivedAmount ?? 0m) + quantity * localCurrencyPrice;
            balances[balanceKey] = quantityBefore + quantity;

            if (lotNumber is not null)
            {
                var lotKey = (item.Id, lotNumber, warehouse.Id, (Guid?)inventoryBin.Id);
                if (!lots.TryGetValue(lotKey, out var lot))
                {
                    lot = await _db.InventoryLots.SingleOrDefaultAsync(existingLot =>
                        existingLot.SkuId == item.Id &&
                        existingLot.LotNumber == lotNumber &&
                        existingLot.WarehouseId == warehouse.Id &&
                        existingLot.BinLocationId == inventoryBin.Id);
                    if (lot is null)
                    {
                        lot = new InventoryLot
                        {
                            SkuId = item.Id,
                            LotNumber = lotNumber,
                            Qty = 0,
                            WarehouseId = warehouse.Id,
                            BinLocationId = inventoryBin.Id,
                            Status = requiresIqc ? "quarantine" : "active"
                        };
                        _db.InventoryLots.Add(lot);
                    }
                    lots.Add(lotKey, lot);
                }

                lot.Qty += quantity;
                if (requiresIqc)
                    lot.Status = "quarantine";
            }
        }

        gr.IqcStatus = hasIqcRequiredItems ? "pending" : "not_required";
        _db.GoodsReceipts.Add(gr);
        if (linkedAsn is not null)
        {
            var receivedAfterThisReceipt = selectedItems
                .GroupBy(item => (
                    item.PurchaseOrderItem.SkuId,
                    LotNumber: NormalizeLotNumber(item.Request.LotNumber)))
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.Request.Quantity));
            var fullyReceived = linkedAsn.Items
                .GroupBy(item => (item.SkuId, LotNumber: NormalizeLotNumber(item.LotNumber)))
                .All(group =>
                {
                    var alreadyReceived = previousAsnReceiptsBySkuAndLot.GetValueOrDefault(group.Key);
                    var receivedNow = receivedAfterThisReceipt.GetValueOrDefault(group.Key);
                    return alreadyReceived + receivedNow >= group.Sum(item => item.ShippedQty);
                });
            linkedAsn.Status = fullyReceived ? "received" : "partially_received";
        }
        po.Status = po.Items.All(item => item.ReceivedQty >= item.Quantity)
            ? "received"
            : "partial_received";
        await _db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();

        return $"收貨完成，收貨單號: {grNumber}";
    }

    private static string? NormalizeLotNumber(string? lotNumber) =>
        string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim();

    public async Task DeletePurchaseOrderAsync(Guid id)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("找不到採購單");

        if (po.Status == "confirmed" || po.Status == "received")
            throw new InvalidOperationException("已確認或已收貨的採購單不可刪除");

        _db.PurchaseOrderItems.RemoveRange(po.Items);
        _db.PurchaseOrders.Remove(po);
        await _db.SaveChangesAsync();
    }

private async Task<PurchaseOrder?> FindPurchaseOrderAsync(string idOrNumber)
    {
        if (Guid.TryParse(idOrNumber, out var gid))
            return await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == gid);
        return await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.PoNumber == idOrNumber);
    }

    private async Task<PurchaseOrder?> FindPurchaseOrderWithItemsAsync(string idOrNumber)
    {
        if (Guid.TryParse(idOrNumber, out var gid))
            return await _db.PurchaseOrders.Include(p => p.Items).ThenInclude(item => item.Sku).FirstOrDefaultAsync(x => x.Id == gid);
        return await _db.PurchaseOrders.Include(p => p.Items).ThenInclude(item => item.Sku).FirstOrDefaultAsync(x => x.PoNumber == idOrNumber);
    }

    private async Task<PurchaseOrder?> LockPurchaseOrderAsync(Guid purchaseOrderId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var purchaseOrder = await _db.PurchaseOrders
                .FromSqlInterpolated($"SELECT * FROM \"PurchaseOrders\" WHERE \"Id\" = {purchaseOrderId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (purchaseOrder != null)
                await _db.Entry(purchaseOrder).ReloadAsync();
            return purchaseOrder;
        }

        return await _db.PurchaseOrders.FirstOrDefaultAsync(order => order.Id == purchaseOrderId);
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (sku != null)
                await _db.Entry(sku).ReloadAsync();
            return sku;
        }

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == skuId);
    }

    // ═══════════════════════════════════════════════
    //  PURCHASE REQUISITION
    // ═══════════════════════════════════════════════

    public async Task<(List<PurchaseRequisitionDto> Items, int TotalCount)> GetPurchaseRequisitionsAsync(string? status, string? searchTerm, int page, int pageSize)
    {
        var query = _db.PurchaseRequisitions
            .Include(r => r.Department)
            .Include(r => r.Requester)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r => r.PrNumber.Contains(searchTerm) || (r.Purpose != null && r.Purpose.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.PrDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PurchaseRequisitionDto
            {
                Id = r.Id,
                PrNumber = r.PrNumber,
                RequestDate = r.PrDate,
                DepartmentId = r.DepartmentId,
                DepartmentName = r.Department != null ? r.Department.DeptName : null,
                RequesterId = r.RequesterId,
                RequesterName = r.Requester != null ? r.Requester.ChineseName : null,
                ProjectCode = r.ProjectCode,
                Purpose = r.Purpose,
                Priority = r.Priority,
                BudgetAccount = r.BudgetAccount,
                Status = r.Status,
                IsLocked = r.IsLocked,
                TotalEstimatedAmount = r.TotalEstimatedAmount,
                CreatedAt = r.CreatedAt,
                ItemCount = r.Items.Count
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PurchaseRequisitionDto?> GetPurchaseRequisitionAsync(Guid id)
    {
        var r = await _db.PurchaseRequisitions
            .Include(r => r.Department)
            .Include(r => r.Requester)
            .Include(r => r.Items).ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return null;

        return new PurchaseRequisitionDto
        {
            Id = r.Id,
            PrNumber = r.PrNumber,
            RequestDate = r.PrDate,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.DeptName,
            RequesterId = r.RequesterId,
            RequesterName = r.Requester?.ChineseName,
            ProjectCode = r.ProjectCode,
            Purpose = r.Purpose,
            Priority = r.Priority,
            BudgetAccount = r.BudgetAccount,
            Status = r.Status,
            IsLocked = r.IsLocked,
            TotalEstimatedAmount = r.TotalEstimatedAmount,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new PurchaseRequisitionItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit ?? "PCS",
                EstimatedUnitPrice = i.EstimatedUnitPrice,
                Currency = i.Currency,
                RequiredDate = i.RequiredDate,
                Notes = i.Notes
            }).ToList()
        };
    }

    public async Task<PurchaseRequisitionDto> CreatePurchaseRequisitionAsync(CreatePurchaseRequisitionRequest req)
    {
        var pr = new PurchaseRequisition
        {
            Id = Guid.NewGuid(),
            PrNumber = DocumentNumberGenerator.GeneratePurchaseRequisition(),
            PrDate = req.PrDate ?? DateTime.UtcNow,
            DepartmentId = req.DepartmentId,
            RequesterId = req.RequesterId,
            ProjectCode = req.ProjectCode,
            Purpose = req.Purpose,
            Priority = req.Priority ?? "normal",
            BudgetAccount = req.BudgetAccount,
            Status = "DRAFT"
        };

        decimal total = 0;
        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                pr.Items.Add(new PurchaseRequisitionItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequisitionId = pr.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    EstimatedUnitPrice = item.EstimatedUnitPrice,
                    Currency = item.Currency ?? "TWD",
                    RequiredDate = item.RequiredDate,
                    Notes = item.Notes
                });
                total += item.Quantity * item.EstimatedUnitPrice;
            }
        }
        pr.TotalEstimatedAmount = total;

        _db.PurchaseRequisitions.Add(pr);
        await _db.SaveChangesAsync();

        return new PurchaseRequisitionDto
        {
            Id = pr.Id,
            PrNumber = pr.PrNumber,
            RequestDate = pr.PrDate,
            Status = pr.Status,
            TotalEstimatedAmount = pr.TotalEstimatedAmount,
            CreatedAt = pr.CreatedAt
        };
    }

    public async Task UpdatePurchaseRequisitionAsync(Guid id, UpdatePurchaseRequisitionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.IsLocked || (pr.Status != "pending" && pr.Status != "draft" && pr.Status != "RETURNED"))
            throw new InvalidOperationException("Cannot edit PR in its current state");

        if (req.PrDate.HasValue) pr.PrDate = req.PrDate.Value;
        if (req.DepartmentId.HasValue) pr.DepartmentId = req.DepartmentId;
        if (req.RequesterId.HasValue) pr.RequesterId = req.RequesterId;
        if (req.ProjectCode != null) pr.ProjectCode = req.ProjectCode;
        if (req.Purpose != null) pr.Purpose = req.Purpose;
        if (req.Priority != null) pr.Priority = req.Priority;
        if (req.BudgetAccount != null) pr.BudgetAccount = req.BudgetAccount;

        if (req.Items != null)
        {
            _db.PurchaseRequisitionItems.RemoveRange(pr.Items);
            pr.Items.Clear();
            decimal total = 0;
            foreach (var item in req.Items)
            {
                pr.Items.Add(new PurchaseRequisitionItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequisitionId = pr.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "PCS",
                    EstimatedUnitPrice = item.EstimatedUnitPrice,
                    Currency = item.Currency ?? "TWD",
                    RequiredDate = item.RequiredDate,
                    Notes = item.Notes
                });
                total += item.Quantity * item.EstimatedUnitPrice;
            }
            pr.TotalEstimatedAmount = total;
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");
        pr.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task ToggleLockAsync(Guid id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");
        pr.IsLocked = !pr.IsLocked;
        await _db.SaveChangesAsync();
    }

    public async Task SubmitPRAsync(Guid id, ApprovalWorkflowService approvalService)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT PR can be submitted");
        if (pr.TotalEstimatedAmount <= 0)
            throw new InvalidOperationException("PR must have items with estimated amount");

        var chainResult = await approvalService.GetApprovalChainAsync("PR", pr.TotalEstimatedAmount);
        pr.WorkflowStatus = "SUBMITTED";
        pr.Status = "SUBMITTED";
        pr.CurrentLevel = chainResult.TotalLevels > 0 ? 1 : 0;
        await _db.SaveChangesAsync();
    }

    public async Task ApprovePRAsync(Guid id, ApprovalActionRequest req, ApprovalWorkflowService approvalService)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            throw new InvalidOperationException("PR is not pending approval");
        if (req.EmployeeId == Guid.Empty)
            throw new ArgumentException("Employee ID required");

        pr.WorkflowStatus = "APPROVED";
        pr.Status = "APPROVED";

        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "APPROVE",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task RejectPRAsync(Guid id, ApprovalActionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            throw new InvalidOperationException("PR is not pending approval");
        if (req.EmployeeId == Guid.Empty)
            throw new ArgumentException("Employee ID required");

        pr.WorkflowStatus = "REJECTED";
        pr.Status = "REJECTED";

        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "REJECT",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task ReturnPRForEditAsync(Guid id, ApprovalActionRequest req)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.WorkflowStatus != "SUBMITTED" && pr.WorkflowStatus != "UNDER_REVIEW")
            throw new InvalidOperationException("PR is not pending approval");
        if (req.EmployeeId == Guid.Empty)
            throw new ArgumentException("Employee ID required");

        pr.WorkflowStatus = "RETURNED_FOR_EDITS";
        pr.Status = "RETURNED";

        _db.PurchaseRequisitionApprovals.Add(new PurchaseRequisitionApproval
        {
            PurchaseRequisitionId = pr.Id,
            ApprovalLevel = pr.CurrentLevel,
            ApproverId = req.EmployeeId,
            Action = "RETURN",
            Comments = req.Comments,
            ActionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<ApprovalChainResult> GetApprovalChainAsync(Guid id, ApprovalWorkflowService approvalService)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id)
            ?? throw new KeyNotFoundException("PR not found");
        return await approvalService.GetApprovalChainAsync("PR", pr.TotalEstimatedAmount);
    }

    public async Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(Guid id)
    {
        return await _db.PurchaseRequisitionApprovals
            .Include(a => a.Approver)
            .Where(a => a.PurchaseRequisitionId == id)
            .OrderBy(a => a.ActionDate)
            .Select(a => new ApprovalHistoryDto
            {
                Id = a.Id,
                Level = a.ApprovalLevel,
                ApproverName = a.Approver != null ? a.Approver.ChineseName : null,
                Action = a.Action,
                Comments = a.Comments,
                ActionDate = a.ActionDate
            })
            .ToListAsync();
    }

    public async Task<string> ConvertToPOAsync(Guid id, ConvertToPoRequest req)
    {
        var pr = await _db.PurchaseRequisitions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PR not found");

        if (pr.Status != "APPROVED" && pr.WorkflowStatus != "APPROVED")
            throw new InvalidOperationException("Only APPROVED PR can be converted to PO");
        if (!pr.Items.Any())
            throw new InvalidOperationException("PR has no items");

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = DocumentNumberGenerator.GeneratePurchaseOrder(),
            PoDate = DateTime.UtcNow,
            SupplierId = req.SupplierId,
            PrId = pr.Id,
            Notes = pr.Purpose,
            Status = "pending",
            TotalAmount = 0
        };

        decimal total = 0;
        foreach (var item in pr.Items)
        {
            po.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.EstimatedUnitPrice
            });
            total += item.Quantity * item.EstimatedUnitPrice;
        }
        po.TotalAmount = total;

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        return $"{po.Id}: {po.PoNumber}";
    }
}
