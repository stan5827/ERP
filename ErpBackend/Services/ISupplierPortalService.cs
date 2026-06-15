using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErpBackend.DTOs;
using ErpBackend.Models.Portal;
namespace ErpBackend.Services;

public interface ISupplierPortalService
{
    Task<SupplierPortalLoginResponse?> LoginAsync(SupplierPortalLoginRequest request);
    Task<IEnumerable<RfqDto>> GetActiveRfqsAsync(Guid supplierId);
    Task<bool> SubmitQuoteAsync(Guid supplierUserId, Guid supplierId, SubmitSupplierQuoteRequest request);
    Task<IEnumerable<PurchaseOrderDto>> GetActivePurchaseOrdersAsync(Guid supplierId);
    Task<bool> ConfirmPurchaseOrderAsync(Guid poId, Guid supplierId);
    Task<IEnumerable<SupplierAsnDto>> GetAsnsAsync(Guid supplierId);
    Task<Guid> CreateAsnAsync(Guid supplierId, CreateSupplierAsnRequest request);
    Task<SupplierPerformanceReportDto?> GetPerformanceScoreAsync(Guid supplierId, string period);
}
