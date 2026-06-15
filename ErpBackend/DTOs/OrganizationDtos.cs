namespace ErpBackend.DTOs;

public class DepartmentTreeDto
{
    public Guid Id { get; set; }
    public string DeptCode { get; set; } = null!;
    public string DeptName { get; set; } = null!;
    public string? DeptType { get; set; }
    public string? ManagerEmployeeName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public List<DepartmentTreeDto> Children { get; set; } = new();
}





public class JobGradeDto
{
    public Guid Id { get; set; }
    public string GradeCode { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal TravelLimit { get; set; }
    public decimal ApprovalLimit { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateJobGradeRequest
{
    public string GradeCode { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal OvertimeRate { get; set; } = 1.33m;
    public decimal TravelLimit { get; set; }
    public decimal ApprovalLimit { get; set; }
    public int Level { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateJobGradeRequest
{
    public string? GradeCode { get; set; }
    public string? GradeName { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public decimal? OvertimeRate { get; set; }
    public decimal? TravelLimit { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public int? Level { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class JobTitleDto
{
    public Guid Id { get; set; }
    public string TitleCode { get; set; } = null!;
    public string TitleName { get; set; } = null!;
    public Guid? GradeId { get; set; }
    public string? GradeCode { get; set; }
    public string? GradeName { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool IsSupervisor { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateJobTitleRequest
{
    public string TitleCode { get; set; } = null!;
    public string TitleName { get; set; } = null!;
    public Guid? GradeId { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool IsSupervisor { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateJobTitleRequest
{
    public string? TitleCode { get; set; }
    public string? TitleName { get; set; }
    public Guid? GradeId { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool? IsSupervisor { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class ApprovalAuthorityDto
{
    public Guid Id { get; set; }
    public string DocType { get; set; } = null!;
    public string DocTypeName { get; set; } = null!;
    public Guid GradeId { get; set; }
    public string GradeCode { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int ApprovalLevel { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateApprovalAuthorityRequest
{
    public string DocType { get; set; } = null!;
    public Guid GradeId { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int ApprovalLevel { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateApprovalAuthorityRequest
{
    public Guid? GradeId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? ApprovalLevel { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

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
    public string GradeCode { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string? Description { get; set; }
}

public class ApprovalCheckResult
{
    public bool CanApprove { get; set; }
    public int? UserGradeLevel { get; set; }
    public decimal? UserApprovalLimit { get; set; }
    public decimal? RequiredAmount { get; set; }
    public string? Message { get; set; }
    public string? NextApproverGrade { get; set; }
    public decimal? NextApprovalLimit { get; set; }
}

public static class DocTypeNames
{
    public static readonly Dictionary<string, string> Names = new()
    {
        { "PO", "採購訂單" },
        { "PR", "採購申請" },
        { "EXPENSE", "費用報支" },
        { "LEAVE", "請假申請" },
        { "OVERTIME", "加班申請" },
        { "BUDGET", "預算申請" },
        { "WORK_ORDER", "工單異動" },
        { "PRICE_CHANGE", "價格異動" },
        { "ASSET", "資產異動" },
        { "CONTRACT", "合約審核" },
    };

    public static string GetName(string docType) => Names.TryGetValue(docType, out var name) ? name : docType;
}
