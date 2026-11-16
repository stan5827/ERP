using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
    public string? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupplierRatingDto
{
    public Guid SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public decimal QualityScore { get; set; }
    public decimal DeliveryScore { get; set; }
    public decimal CostScore { get; set; }
    public decimal ServiceScore { get; set; }
    public decimal TotalScore { get; set; }
    public string? Grade { get; set; }
    public DateTime CalculationDate { get; set; }
    public int PeriodMonths { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class SupplierPortalUserDto
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string? Username { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public string? InvitationStatus { get; set; }
    public DateTime? InvitationExpiresAt { get; set; }
    public DateTime? InvitationSentAt { get; set; }
    public int InvitationAttemptCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Requests a Keycloak invitation for a supplier portal user.</summary>
public sealed record CreateSupplierPortalInvitationRequestDto
{
    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; init; }

    [Required, MaxLength(50)]
    public required string ContactName { get; init; }

    [Required, MaxLength(30)]
    public required string Role { get; init; }
}

/// <summary>Summarizes the result and current state of a supplier portal invitation.</summary>
public sealed record SupplierPortalInvitationResponseDto(
    Guid UserId,
    string Email,
    string InvitationStatus,
    DateTimeOffset? InvitationExpiresAt,
    int AttemptCount);

/// <summary>Provides the portal invitation status of a supplier user without exposing identity-provider identifiers.</summary>
public sealed record SupplierPortalUserStatusResponseDto(
    Guid UserId,
    Guid SupplierId,
    string Email,
    string? ContactName,
    string Role,
    bool IsActive,
    string InvitationStatus,
    DateTimeOffset? InvitationExpiresAt,
    DateTimeOffset? InvitationSentAt);

/// <summary>Confirms that a supplier portal invitation has been activated.</summary>
public sealed record SupplierPortalInvitationCompletedResponseDto(
    string Status,
    string SupplierCode,
    string SupplierName);

public class SupplierApprovalRequest
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid? EmployeeId { get; set; }
    public string? Comments { get; set; }
}

public class CreateSupplierRequest
{
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Currency { get; set; } = "TWD";
}
