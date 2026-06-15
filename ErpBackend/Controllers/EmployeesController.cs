using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class EmployeesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public EmployeesController(ErpGeneralDbContext db) { _db = db; }

    // GET /api/erp/employees
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EmployeeListDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid?   departmentId,
        [FromQuery] string? status,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50)
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

        return Ok(items.Select(ToListDto).ToList(), totalCount: total);
    }

    // GET /api/erp/employees/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Get(Guid id)
    {
        var e = await _db.Set<Employee>()
            .Include(x => x.Department)
            .Include(x => x.JobTitle)
            .Include(x => x.Grade)
            .Include(x => x.Supervisor)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound(ApiHelper.Fail("Employee not found"));
        return Ok(ToDetailDto(e));
    }

    // POST /api/erp/employees
    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Create([FromBody] EmployeeCreateDto req)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Create request received, ChineseName: {req?.ChineseName}");
            
            if (req == null) return BadRequest(ApiHelper.Fail("Invalid request body"));
            
            // Auto-generate employee number if not provided
            if (string.IsNullOrEmpty(req.EmployeeNo))
            {
                var lastNo = await _db.Set<Employee>()
                    .OrderByDescending(e => e.EmployeeNo)
                    .Select(e => e.EmployeeNo)
                    .FirstOrDefaultAsync();
                req.EmployeeNo = GenerateNextEmpNo(lastNo);
                Console.WriteLine($"[DEBUG] Generated employee number: {req.EmployeeNo}");
            }

            if (await _db.Set<Employee>().AnyAsync(e => e.EmployeeNo == req.EmployeeNo))
                return BadRequest(ApiHelper.Fail($"Employee number {req.EmployeeNo} already exists"));

            var emp = MapCreate(req);
            Console.WriteLine($"[DEBUG] Mapped employee, DepartmentId={emp.DepartmentId}, JobTitleId={emp.JobTitleId}, GradeId={emp.GradeId}");
            
            _db.Set<Employee>().Add(emp);
            await _db.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Employee saved with Id={emp.Id}");

            // Reload with navigation
            var created = await _db.Set<Employee>()
                .Include(e => e.Department).Include(e => e.JobTitle).Include(e => e.Grade)
                .FirstAsync(e => e.Id == emp.Id);
            return Ok(ToDetailDto(created), "Employee created");
        }
        catch (Exception ex)
        {
            var errMsg = ex.Message;
            if (ex.InnerException != null) errMsg += " | " + ex.InnerException.Message;
            Console.WriteLine($"[ERROR] Create employee failed: {errMsg}");
            return StatusCode(500, ApiHelper.Fail($"建立員工失敗：{errMsg}"));
        }
    }

    // PUT /api/erp/employees/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> Update(Guid id, [FromBody] EmployeeUpdateDto req)
    {
        try
        {
            var emp = await _db.Set<Employee>()
                .Include(e => e.Department).Include(e => e.JobTitle).Include(e => e.Grade)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (emp == null) return NotFound(ApiHelper.Fail("Employee not found"));

            if (req != null)
            {
                MapUpdate(emp, req);
            }
            emp.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _db.Entry(emp).Reference(e => e.Department).LoadAsync();
            await _db.Entry(emp).Reference(e => e.JobTitle).LoadAsync();
            await _db.Entry(emp).Reference(e => e.Grade).LoadAsync();
            
            return Ok(ToDetailDto(emp), "Employee updated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Update Employee failed: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[INNER ERROR] {ex.InnerException.Message}");
            return StatusCode(500, ApiHelper.Fail($"更新員工失敗：{ex.Message} {(ex.InnerException != null ? " | " + ex.InnerException.Message : "")}"));
        }
    }

    // PATCH /api/erp/employees/{id}/terminate
    [HttpPatch("{id:guid}/terminate")]
    public async Task<ActionResult<ApiResponse>> Terminate(Guid id, [FromBody] TerminateRequest req)
    {
        var emp = await _db.Set<Employee>().FindAsync(id);
        if (emp == null) return NotFound(ApiHelper.Fail("Employee not found"));
        emp.Status            = "terminated";
        emp.ResignationDate   = DateTime.UtcNow;
        emp.ResignationReason = req.Reason;
        emp.UpdatedAt         = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok("Employee terminated");
    }

    // DEBUG: Test endpoint to see raw request
    [HttpPost("debug-update/{id:guid}")]
    public ActionResult<ApiResponse> DebugUpdate(Guid id, [FromBody] Dictionary<string, object> data)
    {
        Console.WriteLine($"[DEBUG] Raw data: {System.Text.Json.JsonSerializer.Serialize(data)}");
        return ApiResponse.Ok("Debug completed");
    }

    // Alternative POST /api/erp/employees/create-simple - simplified endpoint
    [HttpPost("create-simple")]
    public async Task<ActionResult<ApiResponse>> CreateSimple([FromBody] System.Text.Json.JsonElement data)
    {
        try
        {
            var json = data.GetRawText();
            Console.WriteLine($"[DEBUG] CreateSimple raw: {json}");
            var jobj = System.Text.Json.JsonDocument.Parse(json);
            var root = jobj.RootElement;
            
            var lastNo = await _db.Set<Employee>()
                .OrderByDescending(e => e.EmployeeNo)
                .Select(e => e.EmployeeNo)
                .FirstOrDefaultAsync();
            var newEmpNo = GenerateNextEmpNo(lastNo);
            
            var emp = new Employee 
            {
                Id = Guid.NewGuid(),
                EmployeeNo = newEmpNo,
                ChineseName = root.TryGetProperty("chineseName", out var cn) ? cn.ToString() : "",
                EnglishName = root.TryGetProperty("englishName", out var en) && en.ValueKind == System.Text.Json.JsonValueKind.String ? en.ToString() : null,
                NationalId = root.TryGetProperty("nationalId", out var nid) && nid.ValueKind == System.Text.Json.JsonValueKind.String ? nid.ToString() : null,
                Mobile = root.TryGetProperty("mobile", out var mb) && mb.ValueKind == System.Text.Json.JsonValueKind.String ? mb.ToString() : null,
                Phone = root.TryGetProperty("phone", out var ph) && ph.ValueKind == System.Text.Json.JsonValueKind.String ? ph.ToString() : null,
                Email = root.TryGetProperty("email", out var em) && em.ValueKind == System.Text.Json.JsonValueKind.String ? em.ToString() : null,
                Gender = root.TryGetProperty("gender", out var gd) && gd.ValueKind == System.Text.Json.JsonValueKind.String ? gd.ToString() : null,
                HireDate = root.TryGetProperty("hireDate", out var hd) && hd.ValueKind == System.Text.Json.JsonValueKind.String 
                    ? DateTimeOffset.Parse(hd.ToString()).UtcDateTime
                    : DateTime.UtcNow,
                DepartmentId = root.TryGetProperty("departmentId", out var did) && did.ValueKind == System.Text.Json.JsonValueKind.String 
                    ? Guid.Parse(did.ToString()) 
                    : null,
                JobTitleId = root.TryGetProperty("jobTitleId", out var jtid) && jtid.ValueKind == System.Text.Json.JsonValueKind.String 
                    ? Guid.Parse(jtid.ToString()) 
                    : null,
                GradeId = root.TryGetProperty("gradeId", out var gid) && gid.ValueKind == System.Text.Json.JsonValueKind.String 
                    ? Guid.Parse(gid.ToString()) 
                    : null,
                Status = root.TryGetProperty("status", out var st) && st.ValueKind == System.Text.Json.JsonValueKind.String ? st.ToString() : "active",
                BaseSalary = root.TryGetProperty("baseSalary", out var bs) && bs.ValueKind == System.Text.Json.JsonValueKind.Number ? bs.GetDecimal() : 0,
                FoodAllowance = root.TryGetProperty("foodAllowance", out var fa) && fa.ValueKind == System.Text.Json.JsonValueKind.Number ? fa.GetDecimal() : 0,
                PositionAllowance = root.TryGetProperty("positionAllowance", out var pa) && pa.ValueKind == System.Text.Json.JsonValueKind.Number ? pa.GetDecimal() : 0,
                FullAttendanceBonus = root.TryGetProperty("fullAttendanceBonus", out var fab) && fab.ValueKind == System.Text.Json.JsonValueKind.Number ? fab.GetDecimal() : 0,
                LaborInsuranceGrade = root.TryGetProperty("laborInsuranceGrade", out var lig) && lig.ValueKind == System.Text.Json.JsonValueKind.Number ? lig.GetInt32() : 0,
                HealthInsuranceGrade = root.TryGetProperty("healthInsuranceGrade", out var hig) && hig.ValueKind == System.Text.Json.JsonValueKind.Number ? hig.GetInt32() : 0,
                PensionContributionRate = root.TryGetProperty("pensionContributionRate", out var pcr) && pcr.ValueKind == System.Text.Json.JsonValueKind.Number ? pcr.GetDecimal() : 0,
                TaxDependents = root.TryGetProperty("taxDependents", out var td) && td.ValueKind == System.Text.Json.JsonValueKind.Number ? td.GetInt32() : 0,
                OvertimePermission = root.TryGetProperty("overtimePermission", out var op) && op.ValueKind == System.Text.Json.JsonValueKind.True ? true : false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _db.Employees.Add(emp);
            await _db.SaveChangesAsync();
            
            return Ok("Employee created");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateSimple: {ex.Message}");
            Console.WriteLine($"[ERROR] CreateSimple StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner: {ex.InnerException.Message}");
                Console.WriteLine($"[ERROR] Inner StackTrace: {ex.InnerException.StackTrace}");
            }
            return StatusCode(500, ApiResponse.Fail(ex.Message));
        }
    }

    // PATCH /api/erp/employees/{id}/rehire
    [HttpPatch("{id:guid}/rehire")]
    public async Task<ActionResult<ApiResponse>> Rehire(Guid id)
    {
        var emp = await _db.Set<Employee>().FindAsync(id);
        if (emp == null) return NotFound(ApiHelper.Fail("Employee not found"));
        emp.Status          = "active";
        emp.ResignationDate = null;
        emp.UpdatedAt       = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok("Employee rehired");
    }

    // ── helpers ─────────────────────────────────────────────────────────
    private static string GenerateNextEmpNo(string? lastNo)
    {
        if (string.IsNullOrEmpty(lastNo) || !int.TryParse(lastNo.TrimStart('E'), out var num))
            return "E001";
        return $"E{(num + 1):D3}";
    }

    private static Employee MapCreate(EmployeeCreateDto r) => new()
    {
        EmployeeNo              = r.EmployeeNo!,
        ChineseName             = r.ChineseName,
        EnglishName             = r.EnglishName,
        NationalId              = r.NationalId,
        PassportNo              = r.PassportNo,
        BirthDate               = r.BirthDate?.ToUniversalTime(),
        Gender                  = r.Gender,
        Phone                   = r.Phone,
        Mobile                  = r.Mobile,
        Email                   = r.Email,
        RegisteredAddress       = r.RegisteredAddress,
        MailingAddress          = r.MailingAddress,
        EmergencyContactName    = r.EmergencyContactName,
        EmergencyContactPhone   = r.EmergencyContactPhone,
        EmergencyContactRelation = r.EmergencyContactRelation,
        DepartmentId            = r.DepartmentId,
        JobTitleId              = r.JobTitleId,
        GradeId                 = r.GradeId,
        PositionTitle           = r.PositionTitle,
        JobCategory             = r.JobCategory,
        SupervisorId            = r.SupervisorId,
        HireDate                = r.HireDate?.ToUniversalTime(),
        ProbationEndDate        = r.ProbationEndDate?.ToUniversalTime(),
        Status                  = r.Status ?? "active",
        SalaryType              = r.SalaryType,
        BankCode                = r.BankCode,
        BankBranch              = r.BankBranch,
        BankAccount             = r.BankAccount,
        BaseSalary              = r.BaseSalary ?? 0,
        FoodAllowance           = r.FoodAllowance ?? 0,
        PositionAllowance       = r.PositionAllowance ?? 0,
        FullAttendanceBonus     = r.FullAttendanceBonus ?? 0,
        LaborInsuranceGrade     = r.LaborInsuranceGrade ?? 0,
        HealthInsuranceGrade    = r.HealthInsuranceGrade ?? 0,
        PensionContributionRate = r.PensionContributionRate ?? 0,
        TaxDependents           = r.TaxDependents ?? 0,
        CardNumber              = r.CardNumber,
        BiometricId             = r.BiometricId,
        ShiftType               = r.ShiftType,
        OvertimePermission      = r.OvertimePermission ?? false,
        HighestEducation        = r.HighestEducation,
        Major                   = r.Major,
        GraduationYear          = r.GraduationYear,
        ForeignLanguages        = r.ForeignLanguages,
        Skills                  = r.Skills,
    };

    private static void MapUpdate(Employee emp, EmployeeUpdateDto r)
    {
        if (r.ChineseName != null) emp.ChineseName = r.ChineseName;
        if (r.EnglishName != null) emp.EnglishName = r.EnglishName;
        if (r.NationalId != null) emp.NationalId = r.NationalId;
        if (r.PassportNo != null) emp.PassportNo = r.PassportNo;
        if (r.BirthDate != null) emp.BirthDate = r.BirthDate?.ToUniversalTime();
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
        if (r.HireDate != null) emp.HireDate = r.HireDate?.ToUniversalTime();
        if (r.ProbationEndDate != null) emp.ProbationEndDate = r.ProbationEndDate?.ToUniversalTime();
        if (r.Status != null) emp.Status = r.Status;
        if (r.SalaryType != null) emp.SalaryType = r.SalaryType;
        if (r.BankCode != null) emp.BankCode = r.BankCode;
        if (r.BankBranch != null) emp.BankBranch = r.BankBranch;
        if (r.BankAccount != null) emp.BankAccount = r.BankAccount;
        if (r.BaseSalary != null) emp.BaseSalary = r.BaseSalary.Value;
        if (r.FoodAllowance != null) emp.FoodAllowance = r.FoodAllowance.Value;
        if (r.PositionAllowance != null) emp.PositionAllowance = r.PositionAllowance.Value;
        if (r.FullAttendanceBonus != null) emp.FullAttendanceBonus = r.FullAttendanceBonus.Value;
        if (r.LaborInsuranceGrade != null) emp.LaborInsuranceGrade = r.LaborInsuranceGrade.Value;
        if (r.HealthInsuranceGrade != null) emp.HealthInsuranceGrade = r.HealthInsuranceGrade.Value;
        if (r.PensionContributionRate != null) emp.PensionContributionRate = r.PensionContributionRate.Value;
        if (r.TaxDependents != null) emp.TaxDependents = r.TaxDependents.Value;
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
        Id             = e.Id,
        EmployeeNo     = e.EmployeeNo,
        ChineseName    = e.ChineseName,
        EnglishName    = e.EnglishName,
        DepartmentId   = e.DepartmentId,
        DepartmentName = e.Department?.DeptName,
        JobTitleId     = e.JobTitleId,
        JobTitleName   = e.JobTitle?.TitleName,
        GradeId        = e.GradeId,
        GradeName      = e.Grade?.GradeName,
        Status         = e.Status,
        HireDate       = e.HireDate,
        Email          = e.Email,
        Mobile         = e.Mobile,
    };

    private static EmployeeDetailDto ToDetailDto(Employee e) => new()
    {
        Id                      = e.Id,
        EmployeeNo              = e.EmployeeNo,
        ChineseName             = e.ChineseName,
        EnglishName             = e.EnglishName,
        NationalId              = e.NationalId,
        PassportNo              = e.PassportNo,
        BirthDate               = e.BirthDate,
        Gender                  = e.Gender,
        Phone                   = e.Phone,
        Mobile                  = e.Mobile,
        Email                   = e.Email,
        RegisteredAddress       = e.RegisteredAddress,
        MailingAddress          = e.MailingAddress,
        EmergencyContactName    = e.EmergencyContactName,
        EmergencyContactPhone   = e.EmergencyContactPhone,
        EmergencyContactRelation = e.EmergencyContactRelation,
        DepartmentId            = e.DepartmentId,
        DepartmentName          = e.Department?.DeptName,
        JobTitleId              = e.JobTitleId,
        JobTitleName            = e.JobTitle?.TitleName,
        GradeId                 = e.GradeId,
        GradeName               = e.Grade?.GradeName,
        PositionTitle           = e.PositionTitle,
        JobCategory             = e.JobCategory,
        SupervisorId            = e.SupervisorId,
        SupervisorName          = e.Supervisor?.ChineseName,
        HireDate                = e.HireDate,
        ProbationEndDate        = e.ProbationEndDate,
        ResignationDate         = e.ResignationDate,
        Status                  = e.Status,
        SalaryType              = e.SalaryType,
        BankCode                = e.BankCode,
        BankBranch              = e.BankBranch,
        BankAccount             = e.BankAccount,
        BaseSalary              = e.BaseSalary,
        FoodAllowance           = e.FoodAllowance,
        PositionAllowance       = e.PositionAllowance,
        FullAttendanceBonus     = e.FullAttendanceBonus,
        LaborInsuranceGrade     = e.LaborInsuranceGrade,
        HealthInsuranceGrade    = e.HealthInsuranceGrade,
        PensionContributionRate = e.PensionContributionRate,
        TaxDependents           = e.TaxDependents,
        CardNumber              = e.CardNumber,
        BiometricId             = e.BiometricId,
        ShiftType               = e.ShiftType,
        OvertimePermission      = e.OvertimePermission,
        HighestEducation        = e.HighestEducation,
        Major                   = e.Major,
        GraduationYear          = e.GraduationYear,
        ForeignLanguages        = e.ForeignLanguages,
        Skills                  = e.Skills,
        CreatedAt               = e.CreatedAt,
    };
}

// ── Request DTOs ────────────────────────────────────────────────────────
public class TerminateRequest
{
    public string? Reason { get; set; }
}

public class EmployeeCreateDto
{
    public string?   EmployeeNo              { get; set; }
    public string    ChineseName             { get; set; } = null!;
    public string?   EnglishName             { get; set; }
    public string?   NationalId              { get; set; }
    public string?   PassportNo              { get; set; }
    public DateTime? BirthDate               { get; set; }
    public string?   Gender                  { get; set; }
    public string?   Phone                   { get; set; }
    public string?   Mobile                  { get; set; }
    public string?   Email                   { get; set; }
    public string?   RegisteredAddress       { get; set; }
    public string?   MailingAddress          { get; set; }
    public string?   EmergencyContactName    { get; set; }
    public string?   EmergencyContactPhone   { get; set; }
    public string?   EmergencyContactRelation { get; set; }
    public Guid?     DepartmentId            { get; set; }
    public Guid?     JobTitleId              { get; set; }
    public Guid?     GradeId                 { get; set; }
    public string?   PositionTitle           { get; set; }
    public string?   JobCategory             { get; set; }
    public Guid?     SupervisorId            { get; set; }
    public DateTime? HireDate               { get; set; }
    public DateTime? ProbationEndDate       { get; set; }
    public string?   Status                 { get; set; }
    public string?   SalaryType             { get; set; }
    public string?   BankCode               { get; set; }
    public string?   BankBranch             { get; set; }
    public string?   BankAccount            { get; set; }
    public decimal?  BaseSalary             { get; set; }
    public decimal?  FoodAllowance          { get; set; }
    public decimal?  PositionAllowance      { get; set; }
    public decimal?  FullAttendanceBonus    { get; set; }
    public int?      LaborInsuranceGrade    { get; set; }
    public int?      HealthInsuranceGrade   { get; set; }
    public decimal?  PensionContributionRate { get; set; }
    public int?      TaxDependents          { get; set; }
    public string?   CardNumber             { get; set; }
    public string?   BiometricId            { get; set; }
    public string?   ShiftType              { get; set; }
    public bool?     OvertimePermission     { get; set; }
    public string?   HighestEducation       { get; set; }
    public string?   Major                  { get; set; }
    public int?      GraduationYear         { get; set; }
    public string?   ForeignLanguages       { get; set; }
    public string?   Skills                 { get; set; }
}

public class EmployeeUpdateDto
{
    public string?   ChineseName             { get; set; }
    public string?   EnglishName             { get; set; }
    public string?   NationalId              { get; set; }
    public string?   PassportNo              { get; set; }
    public DateTime? BirthDate               { get; set; }
    public string?   Gender                  { get; set; }
    public string?   Phone                   { get; set; }
    public string?   Mobile                  { get; set; }
    public string?   Email                   { get; set; }
    public string?   RegisteredAddress       { get; set; }
    public string?   MailingAddress          { get; set; }
    public string?   EmergencyContactName    { get; set; }
    public string?   EmergencyContactPhone   { get; set; }
    public string?   EmergencyContactRelation { get; set; }
    public Guid?     DepartmentId            { get; set; }
    public Guid?     JobTitleId              { get; set; }
    public Guid?     GradeId                 { get; set; }
    public string?   PositionTitle           { get; set; }
    public string?   JobCategory             { get; set; }
    public Guid?     SupervisorId            { get; set; }
    public DateTime? HireDate               { get; set; }
    public DateTime? ProbationEndDate       { get; set; }
    public string?   Status                 { get; set; }
    public string?   SalaryType             { get; set; }
    public string?   BankCode               { get; set; }
    public string?   BankBranch             { get; set; }
    public string?   BankAccount            { get; set; }
    public decimal?  BaseSalary             { get; set; }
    public decimal?  FoodAllowance          { get; set; }
    public decimal?  PositionAllowance      { get; set; }
    public decimal?  FullAttendanceBonus    { get; set; }
    public int?      LaborInsuranceGrade    { get; set; }
    public int?      HealthInsuranceGrade   { get; set; }
    public decimal?  PensionContributionRate { get; set; }
    public int?      TaxDependents          { get; set; }
    public string?   CardNumber             { get; set; }
    public string?   BiometricId            { get; set; }
    public string?   ShiftType              { get; set; }
    public bool?     OvertimePermission     { get; set; }
    public string?   HighestEducation       { get; set; }
    public string?   Major                  { get; set; }
    public int?      GraduationYear         { get; set; }
    public string?   ForeignLanguages       { get; set; }
    public string?   Skills                 { get; set; }
}

// ── Response DTOs ────────────────────────────────────────────────────────
public class EmployeeListDto
{
    public Guid      Id             { get; set; }
    public string    EmployeeNo     { get; set; } = "";
    public string    ChineseName    { get; set; } = "";
    public string?   EnglishName    { get; set; }
    public Guid?     DepartmentId   { get; set; }
    public string?   DepartmentName { get; set; }
    public Guid?     JobTitleId     { get; set; }
    public string?   JobTitleName   { get; set; }
    public Guid?     GradeId        { get; set; }
    public string?   GradeName      { get; set; }
    public string    Status         { get; set; } = "active";
    public DateTime? HireDate       { get; set; }
    public string?   Email          { get; set; }
    public string?   Mobile         { get; set; }
}

public class EmployeeDetailDto : EmployeeListDto
{
    public string?   NationalId               { get; set; }
    public string?   PassportNo               { get; set; }
    public DateTime? BirthDate                { get; set; }
    public string?   Gender                   { get; set; }
    public string?   Phone                    { get; set; }
    public string?   RegisteredAddress        { get; set; }
    public string?   MailingAddress           { get; set; }
    public string?   EmergencyContactName     { get; set; }
    public string?   EmergencyContactPhone    { get; set; }
    public string?   EmergencyContactRelation { get; set; }
    public string?   PositionTitle            { get; set; }
    public string?   JobCategory              { get; set; }
    public Guid?     SupervisorId             { get; set; }
    public string?   SupervisorName           { get; set; }
    public DateTime? ProbationEndDate         { get; set; }
    public DateTime? ResignationDate          { get; set; }
    public string?   SalaryType               { get; set; }
    public string?   BankCode                 { get; set; }
    public string?   BankBranch               { get; set; }
    public string?   BankAccount              { get; set; }
    public decimal?  BaseSalary               { get; set; }
    public decimal?  FoodAllowance            { get; set; }
    public decimal?  PositionAllowance        { get; set; }
    public decimal?  FullAttendanceBonus      { get; set; }
    public int?      LaborInsuranceGrade      { get; set; }
    public int?      HealthInsuranceGrade     { get; set; }
    public decimal?  PensionContributionRate  { get; set; }
    public int?      TaxDependents            { get; set; }
    public string?   CardNumber               { get; set; }
    public string?   BiometricId              { get; set; }
    public string?   ShiftType                { get; set; }
    public bool?     OvertimePermission       { get; set; }
    public string?   HighestEducation         { get; set; }
    public string?   Major                    { get; set; }
    public int?      GraduationYear           { get; set; }
    public string?   ForeignLanguages         { get; set; }
    public string?   Skills                   { get; set; }
    public DateTime  CreatedAt                { get; set; }
}
