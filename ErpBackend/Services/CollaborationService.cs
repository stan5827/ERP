using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Collaboration;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface ICollaborationService
{
    Task<List<MeetingRoomDto>> GetMeetingRoomsAsync();
    Task<MeetingRoomDto> CreateMeetingRoomAsync(CreateMeetingRoomRequestDto request);
    Task<List<MeetingReservationDto>> GetReservationsAsync(Dictionary<string, string>? parameters);
    Task<MeetingReservationDto> CreateReservationAsync(CreateMeetingReservationRequestDto request, Guid userId);
    Task<List<CrossDeptTaskDto>> GetTasksAsync(Dictionary<string, string>? parameters);
    Task<CrossDeptTaskDto> CreateTaskAsync(CreateCrossDeptTaskRequestDto request, Guid userId);
    Task<bool> UpdateTaskStatusAsync(Guid id, string status, Guid userId);
    Task<bool> AddTaskReplyAsync(Guid id, CreateCrossDeptTaskReplyRequestDto request, Guid userId);
}

public class CollaborationService : ICollaborationService
{
    private readonly ErpGeneralDbContext _db;

    public CollaborationService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<MeetingRoomDto>> GetMeetingRoomsAsync()
    {
        return await _db.MeetingRooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomName)
            .Select(r => new MeetingRoomDto
            {
                Id = r.Id,
                RoomName = r.RoomName,
                Location = r.Location,
                Capacity = r.Capacity,
                Equipment = r.Equipment,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    public async Task<MeetingRoomDto> CreateMeetingRoomAsync(CreateMeetingRoomRequestDto request)
    {
        var entity = new MeetingRoom
        {
            RoomName = request.RoomName.Trim(),
            Location = request.Location,
            Capacity = request.Capacity,
            Equipment = request.Equipment,
            IsActive = request.IsActive
        };

        _db.MeetingRooms.Add(entity);
        await _db.SaveChangesAsync();

        return new MeetingRoomDto
        {
            Id = entity.Id,
            RoomName = entity.RoomName,
            Location = entity.Location,
            Capacity = entity.Capacity,
            Equipment = entity.Equipment,
            IsActive = entity.IsActive
        };
    }

    public async Task<List<MeetingReservationDto>> GetReservationsAsync(Dictionary<string, string>? parameters)
    {
        var query = _db.MeetingReservations
            .AsNoTracking()
            .Include(r => r.Room)
            .AsQueryable();

        if (parameters != null)
        {
            if (parameters.TryGetValue("roomId", out var roomIdStr) && Guid.TryParse(roomIdStr, out var roomId))
                query = query.Where(r => r.RoomId == roomId);
            if (parameters.TryGetValue("startDate", out var startStr) && DateTime.TryParse(startStr, out var startDate))
                query = query.Where(r => r.EndTime >= startDate);
            if (parameters.TryGetValue("endDate", out var endStr) && DateTime.TryParse(endStr, out var endDate))
                query = query.Where(r => r.StartTime <= endDate);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MeetingReservationDto
            {
                Id = r.Id,
                RoomId = r.RoomId,
                RoomName = r.Room.RoomName,
                Subject = r.Subject,
                ReserverId = r.ReserverId,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Attendees = r.Attendees,
                CateringRequired = r.CateringRequired,
                EquipmentRequired = r.EquipmentRequired,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MeetingReservationDto> CreateReservationAsync(CreateMeetingReservationRequestDto request, Guid userId)
    {
        var room = await _db.MeetingRooms.FirstOrDefaultAsync(r => r.Id == request.RoomId);
        if (room == null)
            throw new ArgumentException("???????");

        var conflict = await _db.MeetingReservations.AnyAsync(r =>
            r.RoomId == request.RoomId &&
            r.StartTime < request.EndTime &&
            r.EndTime > request.StartTime &&
            r.Status != "cancelled");

        if (conflict)
            throw new ArgumentException("???????????????????");

        var entity = new MeetingReservation
        {
            RoomId = request.RoomId,
            Subject = request.Subject,
            ReserverId = userId == Guid.Empty ? Guid.Empty : userId,
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc),
            Attendees = request.Attendees,
            CateringRequired = request.CateringRequired,
            EquipmentRequired = request.EquipmentRequired,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _db.MeetingReservations.Add(entity);
        await _db.SaveChangesAsync();

        return new MeetingReservationDto
        {
            Id = entity.Id,
            RoomId = entity.RoomId,
            RoomName = room.RoomName,
            Subject = entity.Subject,
            ReserverId = entity.ReserverId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Attendees = entity.Attendees,
            CateringRequired = entity.CateringRequired,
            EquipmentRequired = entity.EquipmentRequired,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<List<CrossDeptTaskDto>> GetTasksAsync(Dictionary<string, string>? parameters)
    {
        var query = _db.CrossDeptTasks
            .AsNoTracking()
            .Include(t => t.Replies)
            .AsQueryable();

        if (parameters != null)
        {
            if (parameters.TryGetValue("status", out var status) && !string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);
            if (parameters.TryGetValue("urgency", out var urgency) && !string.IsNullOrWhiteSpace(urgency))
                query = query.Where(t => t.Urgency == urgency);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CrossDeptTaskDto
            {
                Id = t.Id,
                TaskName = t.TaskName,
                Description = t.Description,
                Urgency = t.Urgency,
                InitiatedDeptId = t.InitiatedDeptId,
                InitiatedBy = t.InitiatedBy,
                OwnerDeptId = t.OwnerDeptId,
                OwnerBy = t.OwnerBy,
                Assistants = t.Assistants,
                Status = t.Status,
                AssignDate = t.AssignDate,
                ExpectedCompleteDate = t.ExpectedCompleteDate,
                ActualCompleteDate = t.ActualCompleteDate,
                VerificationNotes = t.VerificationNotes,
                SatisfactionScore = t.SatisfactionScore,
                CreatedAt = t.CreatedAt,
                Replies = t.Replies.Select(r => new CrossDeptTaskReplyDto
                {
                    Id = r.Id,
                    TaskId = r.TaskId,
                    ReplierId = r.ReplierId,
                    ReplyContent = r.ReplyContent,
                    AttachmentUrl = r.AttachmentUrl,
                    CreatedAt = r.CreatedAt
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<CrossDeptTaskDto> CreateTaskAsync(CreateCrossDeptTaskRequestDto request, Guid userId)
    {
        var entity = new CrossDeptTask
        {
            TaskName = request.TaskName.Trim(),
            Description = request.Description,
            Urgency = request.Urgency,
            InitiatedDeptId = request.InitiatedDeptId,
            InitiatedBy = request.InitiatedBy ?? userId,
            OwnerDeptId = request.OwnerDeptId,
            OwnerBy = request.OwnerBy,
            Assistants = request.Assistants,
            Status = request.Status,
            AssignDate = DateTime.SpecifyKind(request.AssignDate, DateTimeKind.Utc),
            ExpectedCompleteDate = request.ExpectedCompleteDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.CrossDeptTasks.Add(entity);
        await _db.SaveChangesAsync();

        return await GetTaskDtoAsync(entity.Id) ?? new CrossDeptTaskDto { Id = entity.Id, TaskName = entity.TaskName };
    }

    public async Task<bool> UpdateTaskStatusAsync(Guid id, string status, Guid userId)
    {
        var entity = await _db.CrossDeptTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return false;

        entity.Status = status;
        if (status == "completed")
            entity.ActualCompleteDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddTaskReplyAsync(Guid id, CreateCrossDeptTaskReplyRequestDto request, Guid userId)
    {
        var task = await _db.CrossDeptTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return false;

        var reply = new CrossDeptTaskReply
        {
            TaskId = id,
            ReplierId = request.ReplierId,
            ReplyContent = request.ReplyContent,
            AttachmentUrl = request.AttachmentUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.CrossDeptTaskReplies.Add(reply);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<CrossDeptTaskDto?> GetTaskDtoAsync(Guid id)
    {
        return await _db.CrossDeptTasks
            .AsNoTracking()
            .Include(t => t.Replies)
            .Where(t => t.Id == id)
            .Select(t => new CrossDeptTaskDto
            {
                Id = t.Id,
                TaskName = t.TaskName,
                Description = t.Description,
                Urgency = t.Urgency,
                InitiatedDeptId = t.InitiatedDeptId,
                InitiatedBy = t.InitiatedBy,
                OwnerDeptId = t.OwnerDeptId,
                OwnerBy = t.OwnerBy,
                Assistants = t.Assistants,
                Status = t.Status,
                AssignDate = t.AssignDate,
                ExpectedCompleteDate = t.ExpectedCompleteDate,
                ActualCompleteDate = t.ActualCompleteDate,
                VerificationNotes = t.VerificationNotes,
                SatisfactionScore = t.SatisfactionScore,
                CreatedAt = t.CreatedAt,
                Replies = t.Replies.Select(r => new CrossDeptTaskReplyDto
                {
                    Id = r.Id,
                    TaskId = r.TaskId,
                    ReplierId = r.ReplierId,
                    ReplyContent = r.ReplyContent,
                    AttachmentUrl = r.AttachmentUrl,
                    CreatedAt = r.CreatedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
