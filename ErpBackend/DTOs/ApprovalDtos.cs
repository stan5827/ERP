using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class ApprovalChainResult
{
    public string DocType { get; set; } = null!;
    public decimal Amount { get; set; }
    public List<ApprovalLevelInfo> Levels { get; set; } = new();
    public int TotalLevels { get; set; }
}

public class ApprovalLevelInfo
{
    public int Level { get; set; }
    public string DocType { get; set; } = null!;
    public Guid GradeId { get; set; }
    public string? GradeCode { get; set; }
    public string? GradeName { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string? Description { get; set; }
}

public class ApprovalCheckResult
{
    public bool CanApprove { get; set; }
    public int? UserGradeLevel { get; set; }
    public decimal? UserApprovalLimit { get; set; }
    public decimal RequiredAmount { get; set; }
    public string? Message { get; set; }
    public string? NextApproverGrade { get; set; }
    public decimal? NextApprovalLimit { get; set; }
}
