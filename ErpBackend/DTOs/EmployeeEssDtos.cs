namespace ErpBackend.DTOs;

/// <summary>Location supplied by the employee app when recording an attendance punch.</summary>
public class EmployeeEssClockRequestDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(-90, 90)]
    public double? Latitude { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(-180, 180)]
    public double? Longitude { get; set; }
}

/// <summary>Employee leave request fields accepted by the employee self-service API.</summary>
public class EmployeeEssCreateLeaveRequestDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(30)]
    public string LeaveType { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required]
    public DateTime? StartDate { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public DateTime? EndDate { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>Leave entitlement visible to the authenticated employee.</summary>
public class EmployeeEssLeaveBalanceDto
{
    public string LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}

public class EmployeeEssPayrollDto
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonus { get; set; }
    public decimal AttendanceBonus { get; set; }
    public decimal GrossPay { get; set; }
    public decimal LaborInsuranceDeduction { get; set; }
    public decimal HealthInsuranceDeduction { get; set; }
    public decimal PensionDeduction { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string? Status { get; set; }
    public DateTime? PaymentDate { get; set; }
}
