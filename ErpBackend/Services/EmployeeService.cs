using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public interface IEmployeeService
{
    Task<ListResult<EmployeeListDto>> GetAllAsync(string? search, Guid? departmentId, string? status, int page, int pageSize);
    Task<EmployeeDetailDto?> GetAsync(Guid id);
    Task<EmployeeDetailDto> CreateAsync(EmployeeCreateDto req);
    Task<EmployeeDetailDto> UpdateAsync(Guid id, EmployeeUpdateDto req, Guid actorId);
    Task TerminateAsync(Guid id, string? reason, DateTime effectiveDate, Guid actorId);
    Task RehireAsync(Guid id, string? reason, Guid actorId);
}

public class EmployeeService : IEmployeeService
{
    private readonly ErpGeneralDbContext _db;

    public EmployeeService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<ListResult<EmployeeListDto>> GetAllAsync(
        string? search, Guid? departmentId, string? status, int page, int pageSize)
    {
        var q = _db.Set<Employee>()
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .Include(e => e.Grade)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            q = q.Where(e => e.ChineseName.Contains(search)
                          || e.EmployeeNo.Contains(search)
                          || (e.Email != null && e.Email.Contains(search)));
        if (departmentId.HasValue)
            q = q.Where(e => e.DepartmentId == departmentId.Value);
        if (!string.IsNullOrEmpty(status))
            q = q.Where(e => e.Status == status);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new ListResult<EmployeeListDto>
        {
            Items = items.Select(ToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDetailDto?> GetAsync(Guid id)
    {
        var e = await _db.Set<Employee>()
            .Include(x => x.Department)
            .Include(x => x.JobTitle)
            .Include(x => x.Grade)
            .Include(x => x.Supervisor)
            .FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? null : ToDetailDto(e);
    }

    public async Task<EmployeeDetailDto> CreateAsync(EmployeeCreateDto req)
    {
        if (req.Status != null
            && !string.Equals(req.Status, "active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("New employees must start in active status; use the lifecycle workflow for subsequent status changes.");

        // Auto-generate employee number if not provided
        if (string.IsNullOrEmpty(req.EmployeeNo))
        {
            var lastNo = await _db.Set<Employee>()
                .OrderByDescending(e => e.EmployeeNo)
                .Select(e => e.EmployeeNo)
                .FirstOrDefaultAsync();
            req.EmployeeNo = GenerateNextEmpNo(lastNo);
        }

        if (await _db.Set<Employee>().AnyAsync(e => e.EmployeeNo == req.EmployeeNo))
            throw new InvalidOperationException($"Employee number {req.EmployeeNo} already exists");

        var emp = MapCreate(req);
        _db.Set<Employee>().Add(emp);
        await _db.SaveChangesAsync();

        // Reload with navigation
        var created = await _db.Set<Employee>()
            .Include(e => e.Department).Include(e => e.JobTitle).Include(e => e.Grade)
            .FirstAsync(e => e.Id == emp.Id);
        return ToDetailDto(created);
    }

    public async Task<EmployeeDetailDto> UpdateAsync(Guid id, EmployeeUpdateDto req, Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new InvalidOperationException("An authenticated actor is required.");

        var emp = await _db.Set<Employee>()
            .Include(e => e.Department).Include(e => e.JobTitle).Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException("Employee not found");

        var originalDepartmentId = emp.DepartmentId;
        var originalJobTitleId = emp.JobTitleId;
        var originalGradeId = emp.GradeId;
        var hasOrganizationChanges =
            (req.DepartmentId.HasValue && req.DepartmentId != originalDepartmentId)
            || (req.JobTitleId.HasValue && req.JobTitleId != originalJobTitleId)
            || (req.GradeId.HasValue && req.GradeId != originalGradeId);
        var changeReason = req.ChangeReason?.Trim();
        if (hasOrganizationChanges
            && (string.IsNullOrWhiteSpace(changeReason) || changeReason.Length > 200))
            throw new InvalidOperationException("A change reason of 1 to 200 characters is required for department, job title, or grade changes.");

        if (req.Status != null && !string.Equals(emp.Status, req.Status, StringComparison.OrdinalIgnoreCase))
            await ChangeStatusAsync(emp, req.Status, req.StatusChangeReason, actorId, null);

        MapUpdate(emp, req);
        var organizationChanges = new List<string>();
        if (originalDepartmentId != emp.DepartmentId)
            organizationChanges.Add($"DepartmentId {originalDepartmentId} -> {emp.DepartmentId}");
        if (originalJobTitleId != emp.JobTitleId)
            organizationChanges.Add($"JobTitleId {originalJobTitleId} -> {emp.JobTitleId}");
        if (originalGradeId != emp.GradeId)
            organizationChanges.Add($"GradeId {originalGradeId} -> {emp.GradeId}");

        if (organizationChanges.Count > 0)
        {
            var linkedUsers = await _db.Users.Where(user => user.EmployeeId == emp.Id).ToListAsync();
            foreach (var user in linkedUsers)
                user.SecurityVersion = checked(user.SecurityVersion + 1);

            _db.AuditLogs.Add(new ErpBackend.Models.System.AuditLog
            {
                Action = "EMPLOYEE_ORGANIZATION_CHANGED",
                EntityType = nameof(Employee),
                EntityId = emp.Id,
                UserId = actorId,
                TableName = "Employees",
                Details = $"{string.Join("; ", organizationChanges)}. Reason: {changeReason}"
            });
        }

        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _db.Entry(emp).Reference(e => e.Department).LoadAsync();
        await _db.Entry(emp).Reference(e => e.JobTitle).LoadAsync();
        await _db.Entry(emp).Reference(e => e.Grade).LoadAsync();

        return ToDetailDto(emp);
    }

    public async Task TerminateAsync(Guid id, string? reason, DateTime effectiveDate, Guid actorId)
    {
        var emp = await _db.Set<Employee>().FindAsync(id)
            ?? throw new KeyNotFoundException("Employee not found");

        if (effectiveDate.Date > DateTime.UtcNow.Date)
            throw new InvalidOperationException("The termination effective date cannot be in the future.");

        await ChangeStatusAsync(emp, "terminated", reason, actorId, effectiveDate.Date);
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RehireAsync(Guid id, string? reason, Guid actorId)
    {
        var emp = await _db.Set<Employee>().FindAsync(id)
            ?? throw new KeyNotFoundException("Employee not found");
        if (emp.Status is not ("terminated" or "inactive" or "resigned"))
            throw new InvalidOperationException("Only terminated or inactive employees can be rehired.");

        await ChangeStatusAsync(emp, "active", reason, actorId, null);
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task ChangeStatusAsync(
        Employee employee,
        string newStatus,
        string? reason,
        Guid actorId,
        DateTime? resignationDate)
    {
        var normalizedStatus = newStatus.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("active" or "leave" or "terminated" or "inactive"))
            throw new InvalidOperationException("Employee status must be active, leave, terminated, or inactive.");
        if (actorId == Guid.Empty)
            throw new InvalidOperationException("An authenticated actor is required.");

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 200)
            throw new InvalidOperationException("A status change reason of 1 to 200 characters is required.");

        var previousStatus = employee.Status;
        if (string.Equals(previousStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Employee already has status '{normalizedStatus}'.");

        employee.Status = normalizedStatus;
        if (normalizedStatus == "terminated")
        {
            employee.ResignationDate = DateTime.SpecifyKind(resignationDate ?? DateTime.UtcNow.Date, DateTimeKind.Utc);
            employee.ResignationReason = normalizedReason;
        }
        else if (normalizedStatus == "active")
        {
            employee.ResignationDate = null;
            employee.ResignationReason = null;
        }

        var linkedUsers = await _db.Users.Where(user => user.EmployeeId == employee.Id).ToListAsync();
        foreach (var user in linkedUsers)
        {
            if (normalizedStatus is "terminated" or "inactive")
                user.IsActive = false;
            user.SecurityVersion = checked(user.SecurityVersion + 1);
        }

        _db.AuditLogs.Add(new ErpBackend.Models.System.AuditLog
        {
            Action = "EMPLOYEE_STATUS_CHANGED",
            EntityType = nameof(Employee),
            EntityId = employee.Id,
            UserId = actorId,
            TableName = "Employees",
            Details = $"Status changed from '{previousStatus}' to '{normalizedStatus}'. Reason: {normalizedReason}"
        });
    }

    // ── Private Helpers ──

    private static string GenerateNextEmpNo(string? lastNo)
    {
        if (string.IsNullOrEmpty(lastNo) || !int.TryParse(lastNo.TrimStart('E'), out var num))
            return "E001";
        return $"E{(num + 1):D3}";
    }

    private static DateTime? AsUtcDate(DateTime? value) =>
        value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;

    private static Employee MapCreate(EmployeeCreateDto r) => new()
    {
        EmployeeNo = r.EmployeeNo!,
        ChineseName = r.ChineseName,
        EnglishName = r.EnglishName,
        NationalId = r.NationalId,
        PassportNo = r.PassportNo,
        BirthDate = AsUtcDate(r.BirthDate),
        Gender = r.Gender,
        Phone = r.Phone,
        Mobile = r.Mobile,
        Email = r.Email,
        RegisteredAddress = r.RegisteredAddress,
        MailingAddress = r.MailingAddress,
        EmergencyContactName = r.EmergencyContactName,
        EmergencyContactPhone = r.EmergencyContactPhone,
        EmergencyContactRelation = r.EmergencyContactRelation,
        DepartmentId = r.DepartmentId,
        JobTitleId = r.JobTitleId,
        GradeId = r.GradeId,
        PositionTitle = r.PositionTitle,
        JobCategory = r.JobCategory,
        SupervisorId = r.SupervisorId,
        HireDate = AsUtcDate(r.HireDate),
        ProbationEndDate = AsUtcDate(r.ProbationEndDate),
        Status = r.Status ?? "active",
        SalaryType = r.SalaryType,
        BankCode = r.BankCode,
        BankBranch = r.BankBranch,
        BankAccount = r.BankAccount,
        BaseSalary = r.BaseSalary ?? 0,
        FoodAllowance = r.FoodAllowance ?? 0,
        PositionAllowance = r.PositionAllowance ?? 0,
        FullAttendanceBonus = r.FullAttendanceBonus ?? 0,
        LaborInsuranceGrade = r.LaborInsuranceGrade ?? 0,
        HealthInsuranceGrade = r.HealthInsuranceGrade ?? 0,
        PensionContributionRate = r.PensionContributionRate ?? 0,
        TaxDependents = r.TaxDependents ?? 0,
        CardNumber = r.CardNumber,
        BiometricId = r.BiometricId,
        ShiftType = r.ShiftType,
        OvertimePermission = r.OvertimePermission ?? false,
        HighestEducation = r.HighestEducation,
        Major = r.Major,
        GraduationYear = r.GraduationYear,
        ForeignLanguages = r.ForeignLanguages,
        Skills = r.Skills,
    };

    private static void MapUpdate(Employee emp, EmployeeUpdateDto r)
    {
        if (r.ChineseName != null) emp.ChineseName = r.ChineseName;
        if (r.EnglishName != null) emp.EnglishName = r.EnglishName;
        if (r.NationalId != null) emp.NationalId = r.NationalId;
        if (r.PassportNo != null) emp.PassportNo = r.PassportNo;
        if (r.BirthDate != null) emp.BirthDate = AsUtcDate(r.BirthDate);
        if (r.Gender != null) emp.Gender = r.Gender;
        if (r.Phone != null) emp.Phone = r.Phone;
        if (r.Mobile != null) emp.Mobile = r.Mobile;
        if (r.Email != null) emp.Email = r.Email;
        if (r.RegisteredAddress != null) emp.RegisteredAddress = r.RegisteredAddress;
        if (r.MailingAddress != null) emp.MailingAddress = r.MailingAddress;
        if (r.EmergencyContactName != null) emp.EmergencyContactName = r.EmergencyContactName;
        if (r.EmergencyContactPhone != null) emp.EmergencyContactPhone = r.EmergencyContactPhone;
        if (r.EmergencyContactRelation != null) emp.EmergencyContactRelation = r.EmergencyContactRelation;
        if (r.DepartmentId != null) emp.DepartmentId = r.DepartmentId;
        if (r.JobTitleId != null) emp.JobTitleId = r.JobTitleId;
        if (r.GradeId != null) emp.GradeId = r.GradeId;
        if (r.PositionTitle != null) emp.PositionTitle = r.PositionTitle;
        if (r.JobCategory != null) emp.JobCategory = r.JobCategory;
        if (r.SupervisorId != null) emp.SupervisorId = r.SupervisorId;
        if (r.HireDate != null) emp.HireDate = AsUtcDate(r.HireDate);
        if (r.ProbationEndDate != null) emp.ProbationEndDate = AsUtcDate(r.ProbationEndDate);
        if (r.SalaryType != null) emp.SalaryType = r.SalaryType;
        if (r.BankCode != null) emp.BankCode = r.BankCode;
        if (r.BankBranch != null) emp.BankBranch = r.BankBranch;
        if (r.BankAccount != null) emp.BankAccount = r.BankAccount;
        if (r.BaseSalary != null) emp.BaseSalary = r.BaseSalary ?? 0;
        if (r.FoodAllowance != null) emp.FoodAllowance = r.FoodAllowance ?? 0;
        if (r.PositionAllowance != null) emp.PositionAllowance = r.PositionAllowance ?? 0;
        if (r.FullAttendanceBonus != null) emp.FullAttendanceBonus = r.FullAttendanceBonus ?? 0;
        if (r.LaborInsuranceGrade != null) emp.LaborInsuranceGrade = (int?)r.LaborInsuranceGrade.Value;
        if (r.HealthInsuranceGrade != null) emp.HealthInsuranceGrade = (int?)r.HealthInsuranceGrade.Value;
        if (r.PensionContributionRate != null) emp.PensionContributionRate = r.PensionContributionRate ?? 0;
        if (r.TaxDependents != null) emp.TaxDependents = (int?)r.TaxDependents.Value;
        if (r.CardNumber != null) emp.CardNumber = r.CardNumber;
        if (r.BiometricId != null) emp.BiometricId = r.BiometricId;
        if (r.ShiftType != null) emp.ShiftType = r.ShiftType;
        if (r.OvertimePermission != null) emp.OvertimePermission = r.OvertimePermission.Value;
        if (r.HighestEducation != null) emp.HighestEducation = r.HighestEducation;
        if (r.Major != null) emp.Major = r.Major;
        if (r.GraduationYear != null) emp.GraduationYear = r.GraduationYear;
        if (r.ForeignLanguages != null) emp.ForeignLanguages = r.ForeignLanguages;
        if (r.Skills != null) emp.Skills = r.Skills;
    }

    private static EmployeeListDto ToListDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNo = e.EmployeeNo,
        ChineseName = e.ChineseName,
        EnglishName = e.EnglishName,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.DeptName,
        JobTitleId = e.JobTitleId,
        JobTitleName = e.JobTitle?.TitleName,
        GradeId = e.GradeId,
        GradeName = e.Grade?.GradeName,
        Status = e.Status,
        HireDate = e.HireDate,
        Email = e.Email,
        Mobile = e.Mobile,
    };

    private static EmployeeDetailDto ToDetailDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNo = e.EmployeeNo,
        ChineseName = e.ChineseName,
        EnglishName = e.EnglishName,
        NationalId = e.NationalId,
        PassportNo = e.PassportNo,
        BirthDate = e.BirthDate,
        Gender = e.Gender,
        Phone = e.Phone,
        Mobile = e.Mobile,
        Email = e.Email,
        RegisteredAddress = e.RegisteredAddress,
        MailingAddress = e.MailingAddress,
        EmergencyContactName = e.EmergencyContactName,
        EmergencyContactPhone = e.EmergencyContactPhone,
        EmergencyContactRelation = e.EmergencyContactRelation,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.DeptName,
        JobTitleId = e.JobTitleId,
        JobTitleName = e.JobTitle?.TitleName,
        GradeId = e.GradeId,
        GradeName = e.Grade?.GradeName,
        PositionTitle = e.PositionTitle,
        JobCategory = e.JobCategory,
        SupervisorId = e.SupervisorId,
        SupervisorName = e.Supervisor?.ChineseName,
        HireDate = e.HireDate,
        ProbationEndDate = e.ProbationEndDate,
        ResignationDate = e.ResignationDate,
        Status = e.Status,
        SalaryType = e.SalaryType,
        BankCode = e.BankCode,
        BankBranch = e.BankBranch,
        BankAccount = e.BankAccount,
        BaseSalary = e.BaseSalary,
        FoodAllowance = e.FoodAllowance,
        PositionAllowance = e.PositionAllowance,
        FullAttendanceBonus = e.FullAttendanceBonus,
        LaborInsuranceGrade = e.LaborInsuranceGrade,
        HealthInsuranceGrade = e.HealthInsuranceGrade,
        PensionContributionRate = e.PensionContributionRate,
        TaxDependents = e.TaxDependents,
        CardNumber = e.CardNumber,
        BiometricId = e.BiometricId,
        ShiftType = e.ShiftType,
        OvertimePermission = e.OvertimePermission,
        HighestEducation = e.HighestEducation,
        Major = e.Major,
        GraduationYear = e.GraduationYear,
        ForeignLanguages = e.ForeignLanguages,
        Skills = e.Skills,
        CreatedAt = e.CreatedAt,
    };
}













