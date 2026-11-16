using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class EmployeeSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public EmployeeSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var employeeNo = parameters.GetValueOrDefault("employee_no")?.ToString();
        var departmentIdStr = parameters.GetValueOrDefault("department_id")?.ToString();
        Guid? departmentId = string.IsNullOrEmpty(departmentIdStr) ? null : Guid.Parse(departmentIdStr);

        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(employeeNo))
            query = query.Where(e => e.EmployeeNo.Contains(employeeNo));
        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        var results = await query.Take(50).Select(e => new
        {
            e.EmployeeNo,
            e.ChineseName,
            e.PositionTitle,
            e.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class DepartmentSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public DepartmentSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var deptCode = parameters.GetValueOrDefault("dept_code")?.ToString();
        var deptName = parameters.GetValueOrDefault("dept_name")?.ToString();

        var query = _db.Departments.AsQueryable();
        if (!string.IsNullOrEmpty(deptCode))
            query = query.Where(d => d.DeptCode.Contains(deptCode));
        if (!string.IsNullOrEmpty(deptName))
            query = query.Where(d => d.DeptName.Contains(deptName));

        var results = await query.Take(50).Select(d => new { d.DeptCode, d.DeptName, d.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class HrReportHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var reportType = parameters.GetValueOrDefault("report_type")?.ToString() ?? "attendance";
        return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) });
    }
}
