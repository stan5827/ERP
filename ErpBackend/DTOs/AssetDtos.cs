using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class AssetDto
{
    public Guid Id { get; set; }
    public string AssetNumber { get; set; } = null!;
    public string AssetName { get; set; } = null!;
    public string? Specification { get; set; }
    public string? Manufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string? LocationName { get; set; }
    public Guid? CustodianDeptId { get; set; }
    public string? CustodianDeptName { get; set; }
    public Guid? CustodianEmployeeId { get; set; }
    public decimal AcquisitionCost { get; set; }
    public DateTime? AcquisitionDate { get; set; }
    public DateTime? CommissionDate { get; set; }
    public int UsefulLife { get; set; }
    public decimal ResidualValue { get; set; }
    public string DepreciationMethod { get; set; } = "straight_line";
    public Guid? PurchaseOrderId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DepreciationRecordDto> Depreciations { get; set; } = new();
    public List<AssetMaintenanceDto> Maintenances { get; set; } = new();
}

public class AssetUpsertDto
{
    public string AssetNumber { get; set; } = null!;
    public string AssetName { get; set; } = null!;
    public string? Specification { get; set; }
    public string? Manufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? CustodianDeptId { get; set; }
    public Guid? CustodianEmployeeId { get; set; }
    public decimal AcquisitionCost { get; set; }
    public DateTime? AcquisitionDate { get; set; }
    public DateTime? CommissionDate { get; set; }
    public int UsefulLife { get; set; }
    public decimal ResidualValue { get; set; }
    public string DepreciationMethod { get; set; } = "straight_line";
    public Guid? PurchaseOrderId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string Status { get; set; } = "active";
}

public class AssetMaintenanceDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string MaintenanceType { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ExternalCost { get; set; }
    public bool IsCapitalized { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? SerialNumber { get; set; }
}

public class AssetMaintenanceUpsertDto
{
    public Guid AssetId { get; set; }
    public string MaintenanceType { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ExternalCost { get; set; }
    public bool IsCapitalized { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? SerialNumber { get; set; }
}
