using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class EcnDto
{
    public Guid Id { get; set; }
    public string EcnNumber { get; set; } = null!;
    public Guid EcrId { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; }
    public List<EcnAffectedBomDto> AffectedBoms { get; set; } = new();
}

public class EcnAffectedBomDto
{
    public Guid Id { get; set; }
    public Guid BomId { get; set; }
    public string ActionType { get; set; } = "update"; // update, obsolete, replace
    public string? BomCode { get; set; }
}

public class CreateEcnRequest
{
    public string? EcnNumber { get; set; }
    public Guid EcrId { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime EffectiveDate { get; set; }
    public List<CreateEcnAffectedBomRequest> AffectedBoms { get; set; } = new();
}

public class CreateEcnAffectedBomRequest
{
    public Guid BomId { get; set; }
    public string ActionType { get; set; } = "update";
}

public class EcnImpactDto
{
    public Guid EcnId { get; set; }
    public decimal TotalLossAmount { get; set; }
    public int AffectedItemCount { get; set; }
    public int AffectedWorkOrderCount { get; set; }
    public int AffectedPurchaseOrderCount { get; set; }
    public DateTime? SuggestedEffectiveDate { get; set; }
    public List<EcnImpactItemDto> Details { get; set; } = new();
    public List<EcnImpactWorkOrderDto> AffectedWorkOrders { get; set; } = new();
}

public class EcnImpactItemDto
{
    public string SkuNumber { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal PotentialLoss { get; set; }
    public string ImpactType { get; set; } = "obsolete"; // obsolete, slow_moving
}

public class EcnImpactWorkOrderDto
{
    public Guid WorkOrderId { get; set; }
    public string WoNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public string ProductSkuLabel { get; set; } = null!;
}
