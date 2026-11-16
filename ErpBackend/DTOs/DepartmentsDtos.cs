using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class DepartmentDto
{
    public Guid    Id             { get; set; }
    public string  DeptCode       { get; set; } = "";
    public string  DeptName       { get; set; } = "";
    public string? DeptType       { get; set; }
    public Guid?   ParentDeptId   { get; set; }
    public string? ParentDeptName { get; set; }
    public string? CostCenterCode { get; set; }
    public string? Description    { get; set; }
    public bool    IsActive       { get; set; }
    public bool    IsCostCenter   { get; set; }
    public decimal? BudgetLimit   { get; set; }
    public decimal? BudgetUsed    { get; set; }
    public int     DisplayOrder   { get; set; }
    public DateTime CreatedAt     { get; set; }
    public List<DepartmentDto> Children { get; set; } = new();
}

public class DepartmentUpsertDto
{
    public string  DeptCode       { get; set; } = null!;
    public string  DeptName       { get; set; } = null!;
    public string? DeptType       { get; set; }
    public Guid?   ParentDeptId   { get; set; }
    public string? CostCenterCode { get; set; }
    public string? Description    { get; set; }
    public bool?   IsActive       { get; set; }
    public bool?   IsCostCenter   { get; set; }
    public decimal? BudgetLimit   { get; set; }
    public int?    DisplayOrder   { get; set; }
}
