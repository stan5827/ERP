using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.HR;

public class Department
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string DeptCode { get; set; } = null!;
    [Required, MaxLength(100)] public string DeptName { get; set; } = null!;
    [MaxLength(20)] public string? DeptType { get; set; } = "admin";
    public Guid? ParentDeptId { get; set; }
    [MaxLength(20)] public string? CostCenterCode { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCostCenter { get; set; } = true;
    [Column(TypeName = "decimal(14,2)")] public decimal? BudgetLimit { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? BudgetUsed { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentDeptId))] public Department? ParentDept { get; set; }
    public ICollection<Department> ChildDepts { get; set; } = new List<Department>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class JobGrade
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(10)] public string GradeCode { get; set; } = null!;
    [Required, MaxLength(50)] public string GradeName { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal MinSalary { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal MaxSalary { get; set; }
    [Column(TypeName = "decimal(4,2)")] public decimal OvertimeRate { get; set; } = 1.33m;
    [Column(TypeName = "decimal(14,2)")] public decimal TravelLimit { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ApprovalLimit { get; set; }
    public int Level { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class JobTitle
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string TitleCode { get; set; } = null!;
    [Required, MaxLength(50)] public string TitleName { get; set; } = null!;
    public Guid? GradeId { get; set; }
    [MaxLength(20)] public string? ApplicableDeptType { get; set; }
    public bool IsSupervisor { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(GradeId))] public JobGrade? Grade { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class ApprovalAuthority
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string DocType { get; set; } = null!;
    [Required] public Guid GradeId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal MinAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal MaxAmount { get; set; }
    public int ApprovalLevel { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(GradeId))] public JobGrade Grade { get; set; } = null!;
}

public class Employee
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string EmployeeNo { get; set; } = null!;
    [Required, MaxLength(50)] public string ChineseName { get; set; } = null!;
    [MaxLength(100)] public string? EnglishName { get; set; }
    [MaxLength(20)] public string? NationalId { get; set; }
    [MaxLength(30)] public string? PassportNo { get; set; }
    [MaxLength(30)] public string? ResidencePermitNo { get; set; }
    public DateTime? ResidencePermitExpiry { get; set; }
    public DateTime? BirthDate { get; set; }
    [MaxLength(10)] public string? Gender { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(30)] public string? Mobile { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(300)] public string? RegisteredAddress { get; set; }
    [MaxLength(300)] public string? MailingAddress { get; set; }
    [MaxLength(50)] public string? EmergencyContactName { get; set; }
    [MaxLength(30)] public string? EmergencyContactPhone { get; set; }
    [MaxLength(50)] public string? EmergencyContactRelation { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? JobTitleId { get; set; }
    public Guid? GradeId { get; set; }
    [MaxLength(100)] public string? PositionTitle { get; set; }
    [MaxLength(20)] public string? JobGrade { get; set; }
    [MaxLength(20)] public string? JobGradeCode { get; set; }
    [MaxLength(50)] public string? JobCategory { get; set; }
    public Guid? SupervisorId { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? ResignationDate { get; set; }
    [MaxLength(200)] public string? ResignationReason { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    [MaxLength(20)] public string? SalaryType { get; set; }
    [MaxLength(10)] public string? BankCode { get; set; }
    [MaxLength(20)] public string? BankBranch { get; set; }
    [MaxLength(30)] public string? BankAccount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal? BaseSalary { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? FoodAllowance { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? PositionAllowance { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? FullAttendanceBonus { get; set; }
    public int? LaborInsuranceGrade { get; set; }
    public int? HealthInsuranceGrade { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? PensionContributionRate { get; set; }
    public int? TaxDependents { get; set; }
    [MaxLength(50)] public string? DisabilityCategory { get; set; }
    [MaxLength(50)] public string? CardNumber { get; set; }
    [MaxLength(50)] public string? BiometricId { get; set; }
    [MaxLength(20)] public string? ShiftType { get; set; }
    public bool OvertimePermission { get; set; }
    [MaxLength(100)] public string? HighestEducation { get; set; }
    [MaxLength(100)] public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    [MaxLength(200)] public string? ForeignLanguages { get; set; }
    [MaxLength(500)] public string? Skills { get; set; }
    [MaxLength(500)] public string? IdCardScanUrl { get; set; }
    [MaxLength(500)] public string? DiplomaScanUrl { get; set; }
    [MaxLength(500)] public string? InsuranceTransferUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DepartmentId))] public Department? Department { get; set; }
    [ForeignKey(nameof(JobTitleId))] public JobTitle? JobTitle { get; set; }
    [ForeignKey(nameof(GradeId))] public JobGrade? Grade { get; set; }
    [ForeignKey(nameof(SupervisorId))] public Employee? Supervisor { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<OvertimeRequest> OvertimeRequests { get; set; } = new List<OvertimeRequest>();
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    public ICollection<EmployeeCertification> Certifications { get; set; } = new List<EmployeeCertification>();
    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
}

public class EmployeeCertification
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(200)] public string CertificationName { get; set; } = null!;
    [MaxLength(100)] public string? CertifyingAuthority { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [MaxLength(200)] public string? CertNumber { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class WorkExperience
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [MaxLength(200)] public string? CompanyName { get; set; }
    [MaxLength(100)] public string? Position { get; set; }
    [MaxLength(100)] public string? Department { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(500)] public string? Responsibilities { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class EmploymentHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(50)] public string ChangeType { get; set; } = null!;
    public Guid? OldDeptId { get; set; }
    public Guid? NewDeptId { get; set; }
    [MaxLength(100)] public string? OldPosition { get; set; }
    [MaxLength(100)] public string? NewPosition { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal? OldSalary { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal? NewSalary { get; set; }
    public DateTime EffectiveDate { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

    public class AttendanceRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    [MaxLength(50)] public string? ProductionLine { get; set; }
    public DateTime Date { get; set; }
    [MaxLength(20)] public string? ShiftType { get; set; }
    public TimeOnly? ScheduledStartTime { get; set; }
    public TimeOnly? ScheduledEndTime { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    [MaxLength(200)] public string? ClockInLocation { get; set; }
    [MaxLength(200)] public string? ClockOutLocation { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "normal";
    public bool LateFlag { get; set; }
    public int LateMinutes { get; set; }
    public bool EarlyLeaveFlag { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public bool ClockingAnomalyFlag { get; set; }
    [MaxLength(50)] public string? AnomalyType { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Ot1Hours { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Ot2Hours { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Ot3Hours { get; set; }
    [MaxLength(500)] public string? OtReason { get; set; }
    [MaxLength(20)] public string ApprovalStatus { get; set; } = "pending";
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
    [ForeignKey(nameof(DepartmentId))] public Department? Department { get; set; }
}

public class LeaveRequest
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [MaxLength(30)] public string ApplicationNo { get; set; } = null!;
    [Required, MaxLength(30)] public string LeaveType { get; set; } = null!;
    public DateTime ApplicationDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal TotalHours { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(500)] public string? AttachmentUrl { get; set; }
    public bool IncludesSpecialPeriod { get; set; }
    public bool ShiftConflictFlag { get; set; }
    public bool AutoDeductPayFlag { get; set; } = true;
    public bool ImpactOtOrBonus { get; set; }
    public int ApprovalLevel { get; set; } = 1;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(500)] public string? RejectReason { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class LeaveBalance
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(30)] public string LeaveType { get; set; } = null!;
    public int Year { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal TotalEntitled { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal Used { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal Remaining { get; set; }

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class OvertimeRequest
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TotalHours { get; set; }
    [MaxLength(30)] public string OvertimeType { get; set; } = "weekday";
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class ShiftSchedule
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(7)] public string YearMonth { get; set; } = null!;
    public string? ScheduleData { get; set; }
    [MaxLength(20)] public string ShiftType { get; set; } = "fixed";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class Payroll
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BasicSalary { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal InsuranceBase { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PensionBase { get; set; }
    public bool NewLeavingFlag { get; set; }
    public int ScheduledDays { get; set; }
    public int ActualWorkingDays { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyMinutes { get; set; }
    public int AnomalyDays { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TotalOt1Hours { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TotalOt2Hours { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TotalOt3Hours { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Ot1Pay { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Ot2Pay { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Ot3Pay { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal TotalLeaveHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal AnnualLeaveHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal PersonalLeaveHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal SickLeaveHours { get; set; }
    public bool FullAttendanceFlag { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LateDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EarlyDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AnomalyPenalty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PersonalLeaveDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SickLeaveDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal OtherDeductions { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal OvertimePay { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Bonus { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AttendanceBonus { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LaborInsuranceDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal HealthInsuranceDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PensionDeduction { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal WithholdingTax { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? SupplementalHealthInsurance { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EmployerLi { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EmployerHi { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EmployerPf { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EmploymentInsurance { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal GrossPay { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalDeductions { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EmployerContributions { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal NetPay { get; set; }
    [MaxLength(50)] public string? CalculatedBy { get; set; }
    public DateTime? CalculatedAt { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTime? PaymentDate { get; set; }
    [MaxLength(30)] public string? PaymentBankAccount { get; set; }
    [MaxLength(30)] public string? PayrollSlipNo { get; set; }
    [MaxLength(500)] public string? CostAllocation { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
    public ICollection<PayrollDetail> Details { get; set; } = new List<PayrollDetail>();
}

public class PayrollDetail
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid PayrollId { get; set; }
    [Required, MaxLength(20)] public string ItemType { get; set; } = null!;
    [Required, MaxLength(50)] public string ItemCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ItemName { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal Amount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? CalculationBase { get; set; }
    [MaxLength(10)] public string? CalculationUnit { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? Quantity { get; set; }
    [MaxLength(10)] public string? TaxType { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(PayrollId))] public Payroll Payroll { get; set; } = null!;
}

public class PayrollAnomaly
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    public Guid? PayrollId { get; set; }
    [Required, MaxLength(50)] public string AnomalyCategory { get; set; } = null!;
    public string? Description { get; set; }
    [MaxLength(500)] public string? AttachmentUrl { get; set; }
    [MaxLength(30)] public string? ExpectedResolution { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class PayrollCorrection
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid AnomalyId { get; set; }
    [MaxLength(50)] public string? VerificationResult { get; set; }
    public string? AdjustmentItems { get; set; }
    public string? AccountingPreview { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? CurrentApproverId { get; set; }
    public string? ApprovalFlow { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AnomalyId))] public PayrollAnomaly Anomaly { get; set; } = null!;
}

public class PerformanceReview
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(20)] public string ReviewPeriod { get; set; } = null!;
    [MaxLength(30)] public string ReviewType { get; set; } = "KPI";
    [Column(TypeName = "decimal(5,2)")] public decimal? Score { get; set; }
    public string? Comments { get; set; }
    [Required] public Guid ReviewerId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class TrainingRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(200)] public string TrainingName { get; set; } = null!;
    [MaxLength(100)] public string? TrainingProvider { get; set; }
    public DateTime TrainingDate { get; set; }
    public int TrainingHours { get; set; }
    [MaxLength(20)] public string? CertificationResult { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}
