using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class IqcReportDto
{
    public Guid Id { get; set; }
    public string ReportNumber { get; set; } = null!;
    public Guid GoodsReceiptId { get; set; }
    public string? GoodsReceiptNumber { get; set; }
    public string? SupplierName { get; set; }
    public Guid? SkuId { get; set; }
    public string? SkuName { get; set; }
    public string? BatchNumber { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal SampleQty { get; set; }
    public decimal DefectQty { get; set; }
    public string Result { get; set; } = null!;
    public DateTime ReportDate { get; set; }
}

public class EcnDto
{
    public Guid Id { get; set; }
    public string EcnNumber { get; set; } = null!;
    public Guid? EcrId { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<EcnAffectedBomDto> AffectedBoms { get; set; } = new();
}

public class EcnAffectedBomDto
{
    public Guid Id { get; set; }
    public Guid BomId { get; set; }
    public string ActionType { get; set; } = null!;
}

public class CreateEcnRequest
{
    public string? EcnNumber { get; set; }
    public Guid? EcrId { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public List<CreateEcnAffectedBomRequest> AffectedBoms { get; set; } = new();
}

public class CreateEcnAffectedBomRequest
{
    public Guid BomId { get; set; }
    public string ActionType { get; set; } = null!;
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
    public int CurrentStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal PotentialLoss { get; set; }
    public string ImpactType { get; set; } = null!;
}

public class EcnImpactWorkOrderDto
{
    public Guid WorkOrderId { get; set; }
    public string WoNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public string ProductSkuLabel { get; set; } = null!;
}

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public string ApplicationNo { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string LeaveType { get; set; } = null!;
    public DateTime ApplicationDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal TotalHours { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IncludesSpecialPeriod { get; set; }
    public bool ShiftConflictFlag { get; set; }
    public bool AutoDeductPayFlag { get; set; }
    public bool ImpactOtOrBonus { get; set; }
    public int ApprovalLevel { get; set; }
    public string Status { get; set; } = null!;
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeaveRequestRequest
{
    public string EmployeeId { get; set; } = null!;
    public string LeaveType { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IncludesSpecialPeriod { get; set; }
    public bool ImpactOtOrBonus { get; set; }
}

public class ApproveLeaveRequestRequest
{
    public Guid ApproverId { get; set; }
    public string? Remarks { get; set; }
}

public class RejectLeaveRequestRequest
{
    public Guid ApproverId { get; set; }
    public string? RejectReason { get; set; }
}

public class PayrollDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal InsuranceBase { get; set; }
    public decimal PensionBase { get; set; }
    public int ScheduledDays { get; set; }
    public int ActualWorkingDays { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyMinutes { get; set; }
    public int AnomalyDays { get; set; }
    public decimal TotalOt1Hours { get; set; }
    public decimal TotalOt2Hours { get; set; }
    public decimal TotalOt3Hours { get; set; }
    public decimal Ot1Pay { get; set; }
    public decimal Ot2Pay { get; set; }
    public decimal Ot3Pay { get; set; }
    public decimal TotalLeaveHours { get; set; }
    public decimal AnnualLeaveHours { get; set; }
    public decimal PersonalLeaveHours { get; set; }
    public decimal SickLeaveHours { get; set; }
    public bool FullAttendanceFlag { get; set; }
    public decimal LateDeduction { get; set; }
    public decimal EarlyDeduction { get; set; }
    public decimal AnomalyPenalty { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonus { get; set; }
    public decimal AttendanceBonus { get; set; }
    public decimal LaborInsuranceDeduction { get; set; }
    public decimal HealthInsuranceDeduction { get; set; }
    public decimal PensionDeduction { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal EmployerContributions { get; set; }
    public string? PaymentBankAccount { get; set; }
    public string? PayrollSlipNo { get; set; }
    public string? CostAllocation { get; set; }
    public string? Notes { get; set; }
}

public class PayrollSummaryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalEmployerContributions { get; set; }
}

public class CalculatePayrollRequest
{
    public Guid EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class JobGradeDto
{
    public Guid Id { get; set; }
    public string GradeCode { get; set; } = "";
    public string GradeName { get; set; } = "";
    public int Level { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal TravelLimit { get; set; }
    public decimal ApprovalLimit { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobGradeUpsertDto
{
    public string GradeCode { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public int? Level { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public decimal? OvertimeRate { get; set; }
    public decimal? TravelLimit { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class TerminateRequest
{
    public string? Reason { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class EmployeeStatusReasonDto
{
    public string? Reason { get; set; }
}
