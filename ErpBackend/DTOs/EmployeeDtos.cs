using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class EmployeeListDto
{
    public Guid Id { get; set; }
    public string? EmployeeNo { get; set; }
    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? JobTitleId { get; set; }
    public string? JobTitleName { get; set; }
    public Guid? GradeId { get; set; }
    public string? GradeName { get; set; }
    public string? Status { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

public class EmployeeDetailDto
{
    public Guid Id { get; set; }
    public string? EmployeeNo { get; set; }
    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNo { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? RegisteredAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? JobTitleId { get; set; }
    public string? JobTitleName { get; set; }
    public Guid? GradeId { get; set; }
    public string? GradeName { get; set; }
    public string? PositionTitle { get; set; }
    public string? JobCategory { get; set; }
    public Guid? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? ResignationDate { get; set; }
    public string? Status { get; set; }
    public string? SalaryType { get; set; }
    public string? BankCode { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? FoodAllowance { get; set; }
    public decimal? PositionAllowance { get; set; }
    public decimal? FullAttendanceBonus { get; set; }
    public int? LaborInsuranceGrade { get; set; }
    public int? HealthInsuranceGrade { get; set; }
    public decimal? PensionContributionRate { get; set; }
    public int? TaxDependents { get; set; }
    public string? CardNumber { get; set; }
    public string? BiometricId { get; set; }
    public string? ShiftType { get; set; }
    public bool? OvertimePermission { get; set; }
    public string? HighestEducation { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? ForeignLanguages { get; set; }
    public string? Skills { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeCreateDto
{
    public string? EmployeeNo { get; set; }
    public string ChineseName { get; set; } = null!;
    public string? EnglishName { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNo { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? RegisteredAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? JobTitleId { get; set; }
    public Guid? GradeId { get; set; }
    public string? PositionTitle { get; set; }
    public string? JobCategory { get; set; }
    public Guid? SupervisorId { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public string? SalaryType { get; set; }
    public string? BankCode { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? FoodAllowance { get; set; }
    public decimal? PositionAllowance { get; set; }
    public decimal? FullAttendanceBonus { get; set; }
    public int? LaborInsuranceGrade { get; set; }
    public int? HealthInsuranceGrade { get; set; }
    public decimal? PensionContributionRate { get; set; }
    public int? TaxDependents { get; set; }
    public string? CardNumber { get; set; }
    public string? BiometricId { get; set; }
    public string? ShiftType { get; set; }
    public bool? OvertimePermission { get; set; }
    public string? HighestEducation { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? ForeignLanguages { get; set; }
    public string? Skills { get; set; }
    public string? Status { get; set; } = "active";
}

public class EmployeeUpdateDto
{
    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNo { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? RegisteredAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? JobTitleId { get; set; }
    public Guid? GradeId { get; set; }
    public string? PositionTitle { get; set; }
    public string? JobCategory { get; set; }
    public Guid? SupervisorId { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public string? Status { get; set; }
    public string? StatusChangeReason { get; set; }
    public string? ChangeReason { get; set; }
    public string? SalaryType { get; set; }
    public string? BankCode { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? FoodAllowance { get; set; }
    public decimal? PositionAllowance { get; set; }
    public decimal? FullAttendanceBonus { get; set; }
    public int? LaborInsuranceGrade { get; set; }
    public int? HealthInsuranceGrade { get; set; }
    public decimal? PensionContributionRate { get; set; }
    public int? TaxDependents { get; set; }
    public string? CardNumber { get; set; }
    public string? BiometricId { get; set; }
    public string? ShiftType { get; set; }
    public bool? OvertimePermission { get; set; }
    public string? HighestEducation { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? ForeignLanguages { get; set; }
    public string? Skills { get; set; }
}





