using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class MeetingRoomDto
{
    public Guid Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Capacity { get; set; }
    public string? Equipment { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMeetingRoomRequestDto
{
    [Required, MaxLength(50)]
    public string RoomName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? Location { get; set; }
    public int Capacity { get; set; }
    [MaxLength(500)]
    public string? Equipment { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MeetingReservationDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? Subject { get; set; }
    public Guid ReserverId { get; set; }
    public string? ReserverName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Attendees { get; set; }
    public bool CateringRequired { get; set; }
    public string? EquipmentRequired { get; set; }
    public string Status { get; set; } = "confirmed";
    public DateTime CreatedAt { get; set; }
}

public class CreateMeetingReservationRequestDto
{
    [Required]
    public Guid RoomId { get; set; }
    [MaxLength(200)]
    public string? Subject { get; set; }
    [Required]
    public DateTime StartTime { get; set; }
    [Required]
    public DateTime EndTime { get; set; }
    public string? Attendees { get; set; }
    public bool CateringRequired { get; set; }
    public string? EquipmentRequired { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "confirmed";
}

public class CrossDeptTaskDto
{
    public Guid Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Urgency { get; set; } = "normal";
    public Guid? InitiatedDeptId { get; set; }
    public string? InitiatedDeptName { get; set; }
    public Guid? InitiatedBy { get; set; }
    public string? InitiatedByName { get; set; }
    public Guid? OwnerDeptId { get; set; }
    public string? OwnerDeptName { get; set; }
    public Guid? OwnerBy { get; set; }
    public string? OwnerByName { get; set; }
    public string? Assistants { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime AssignDate { get; set; }
    public DateTime? ExpectedCompleteDate { get; set; }
    public DateTime? ActualCompleteDate { get; set; }
    public string? VerificationNotes { get; set; }
    public int? SatisfactionScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CrossDeptTaskReplyDto> Replies { get; set; } = new();
}

public class CreateCrossDeptTaskRequestDto
{
    [Required, MaxLength(200)]
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(20)]
    public string Urgency { get; set; } = "normal";
    public Guid? InitiatedDeptId { get; set; }
    public Guid? InitiatedBy { get; set; }
    public Guid? OwnerDeptId { get; set; }
    public Guid? OwnerBy { get; set; }
    public string? Assistants { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    [Required]
    public DateTime AssignDate { get; set; }
    public DateTime? ExpectedCompleteDate { get; set; }
}

public class UpdateCrossDeptTaskStatusRequestDto
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

public class CrossDeptTaskReplyDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ReplierId { get; set; }
    public string? ReplierName { get; set; }
    public string? ReplyContent { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCrossDeptTaskReplyRequestDto
{
    [Required]
    public Guid ReplierId { get; set; }
    [MaxLength(500)]
    public string? ReplyContent { get; set; }
    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }
}
