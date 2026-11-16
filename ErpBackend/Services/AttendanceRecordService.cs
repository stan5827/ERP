using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using Microsoft.Extensions.Configuration;

namespace ErpBackend.Services;

public interface IAttendanceRecordService
{
    Task<List<AttendanceRecordDto>> GetAttendanceRecordsAsync(
        string? employeeId = null,
        string? departmentId = null,
        string? startDate = null,
        string? endDate = null,
        string? status = null,
        int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<AttendanceRecordDto?> GetAttendanceRecordAsync(Guid id);
    Task<string> ClockInAsync(ClockInRequest req);
    Task<(bool found, string status)> ClockOutAsync(ClockOutRequest req);
    Task<string> ClockInForEmployeeAsync(Guid employeeId, double latitude, double longitude, CancellationToken cancellationToken);
    Task<string> ClockOutForEmployeeAsync(Guid employeeId, double latitude, double longitude, CancellationToken cancellationToken);
    Task<int> BatchImportAsync(BatchAttendanceRequest req);
}

public class AttendanceRecordService : IAttendanceRecordService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IConfiguration _configuration;

    public AttendanceRecordService(ErpGeneralDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<List<AttendanceRecordDto>> GetAttendanceRecordsAsync(
        string? employeeId = null,
        string? departmentId = null,
        string? startDate = null,
        string? endDate = null,
        string? status = null,
        int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AttendanceRecords
            .Include(a => a.Employee)
            .Include(a => a.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var empGuid))
            query = query.Where(a => a.EmployeeId == empGuid);

        if (!string.IsNullOrEmpty(departmentId) && Guid.TryParse(departmentId, out var deptGuid))
            query = query.Where(a => a.DepartmentId == deptGuid);

        if (!string.IsNullOrEmpty(startDate)
            && DateTime.TryParse(startDate, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var start))
            query = query.Where(a => a.Date >= start);

        if (!string.IsNullOrEmpty(endDate)
            && DateTime.TryParse(endDate, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var end))
            query = query.Where(a => a.Date <= end);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.ClockIn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items.Select(a => MapToDto(a)).ToList();
    }

    public async Task<AttendanceRecordDto?> GetAttendanceRecordAsync(Guid id)
    {
        var a = await _db.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        return a == null ? null : MapToDto(a);
    }

    public async Task<string> ClockInAsync(ClockInRequest req)
    {
        if (!Guid.TryParse(req.EmployeeId, out var empId))
            return "invalid";

        var now = DateTime.UtcNow;
        var record = new AttendanceRecord
        {
            EmployeeId = empId,
            Date = now.Date,
            ClockIn = now,
            Status = "present",
            LateFlag = now.Hour >= 9,
            LateMinutes = now.Hour >= 9 ? (int)(now - now.Date.AddHours(9)).TotalMinutes : 0
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return "ok";
    }

    public async Task<(bool found, string status)> ClockOutAsync(ClockOutRequest req)
    {
        if (!Guid.TryParse(req.EmployeeId, out var empId))
            return (false, "invalid");

        var today = DateTime.UtcNow.Date;
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);

        if (record == null) return (false, "not_found");

        var now = DateTime.UtcNow;
        record.ClockOut = now;
        record.EarlyLeaveFlag = now.Hour < 18;
        record.EarlyLeaveMinutes = now.Hour < 18 ? (int)(now.Date.AddHours(18) - now).TotalMinutes : 0;

        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<string> ClockInForEmployeeAsync(
        Guid employeeId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
            return "employee_not_found";

        var locationStatus = ValidateClockLocation(latitude, longitude);
        if (locationStatus != "ok")
            return locationStatus;

        if (!TryGetAttendanceLocalTime(DateTimeOffset.UtcNow, out var localNow))
            return "timezone_invalid";

        var attendanceDate = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Utc);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == attendanceDate, cancellationToken);

        if (record?.ClockIn is not null)
            return "already_clocked_in";

        record ??= new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = attendanceDate
        };

        record.Date = attendanceDate;
        record.ClockIn = localNow.UtcDateTime;
        record.ClockInLocation = FormatLocation(latitude, longitude);
        record.Status = localNow >= localNow.Date.AddHours(9) ? "late" : "normal";
        record.LateFlag = localNow >= localNow.Date.AddHours(9);
        record.LateMinutes = record.LateFlag
            ? (int)(localNow - localNow.Date.AddHours(9)).TotalMinutes
            : 0;

        if (_db.Entry(record).State == EntityState.Detached)
            _db.AttendanceRecords.Add(record);

        await _db.SaveChangesAsync(cancellationToken);
        return "ok";
    }

    public async Task<string> ClockOutForEmployeeAsync(
        Guid employeeId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
            return "employee_not_found";

        var locationStatus = ValidateClockLocation(latitude, longitude);
        if (locationStatus != "ok")
            return locationStatus;

        if (!TryGetAttendanceLocalTime(DateTimeOffset.UtcNow, out var localNow))
            return "timezone_invalid";

        var attendanceDate = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Utc);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == attendanceDate, cancellationToken);

        if (record?.ClockIn is null)
            return "not_found";
        if (record.ClockOut is not null)
            return "already_clocked_out";

        record.ClockOut = localNow.UtcDateTime;
        record.ClockOutLocation = FormatLocation(latitude, longitude);
        record.EarlyLeaveFlag = localNow < localNow.Date.AddHours(18);
        record.EarlyLeaveMinutes = record.EarlyLeaveFlag
            ? (int)(localNow.Date.AddHours(18) - localNow).TotalMinutes
            : 0;

        await _db.SaveChangesAsync(cancellationToken);
        return "ok";
    }

    public async Task<int> BatchImportAsync(BatchAttendanceRequest req)
    {
        foreach (var item in req.Records)
        {
            var record = new AttendanceRecord
            {
                EmployeeId = item.EmployeeId,
                DepartmentId = item.DepartmentId,
                Date = item.Date,
                ShiftType = item.ShiftType,
                ClockIn = item.ClockIn,
                ClockOut = item.ClockOut,
                Status = item.Status ?? "present",
                LateFlag = item.ClockIn.HasValue && item.ClockIn.Value.Hour >= 9,
                LateMinutes = item.ClockIn.HasValue && item.ClockIn.Value.Hour >= 9
                    ? (int)(item.ClockIn.Value - item.ClockIn.Value.Date.AddHours(9)).TotalMinutes : 0,
                ApprovalStatus = "approved"
            };
            _db.AttendanceRecords.Add(record);
        }

        await _db.SaveChangesAsync();
        return req.Records.Count;
    }

    private string ValidateClockLocation(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude)
            || latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return "invalid_location";

        if (!TryGetGeofence(out var centerLatitude, out var centerLongitude, out var radiusMeters))
            return "geofence_not_configured";

        return CalculateDistanceMeters(latitude, longitude, centerLatitude, centerLongitude) <= radiusMeters
            ? "ok"
            : "outside_geofence";
    }

    private bool TryGetGeofence(out double latitude, out double longitude, out double radiusMeters)
    {
        latitude = 0;
        longitude = 0;
        radiusMeters = 0;

        return double.TryParse(_configuration["AttendanceGeofence:Latitude"], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
            && double.TryParse(_configuration["AttendanceGeofence:Longitude"], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)
            && double.TryParse(_configuration["AttendanceGeofence:RadiusMeters"], NumberStyles.Float, CultureInfo.InvariantCulture, out radiusMeters)
            && double.IsFinite(latitude)
            && double.IsFinite(longitude)
            && double.IsFinite(radiusMeters)
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180
            && radiusMeters is > 0 and <= 100_000;
    }

    private bool TryGetAttendanceLocalTime(DateTimeOffset utcNow, out DateTimeOffset localTime)
    {
        try
        {
            var timeZoneId = _configuration["AttendanceGeofence:TimeZoneId"] ?? "Asia/Taipei";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            localTime = TimeZoneInfo.ConvertTime(utcNow, timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            localTime = default;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            localTime = default;
            return false;
        }
    }

    private static double CalculateDistanceMeters(double latitude, double longitude, double centerLatitude, double centerLongitude)
    {
        const double earthRadiusMeters = 6_371_000;
        var latitudeDelta = ToRadians(centerLatitude - latitude);
        var longitudeDelta = ToRadians(centerLongitude - longitude);
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(ToRadians(latitude)) * Math.Cos(ToRadians(centerLatitude))
            * Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        return earthRadiusMeters * 2 * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static string FormatLocation(double latitude, double longitude)
        => $"{latitude.ToString("R", CultureInfo.InvariantCulture)},{longitude.ToString("R", CultureInfo.InvariantCulture)}";

    private static AttendanceRecordDto MapToDto(AttendanceRecord a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee?.ChineseName,
        DepartmentId = a.DepartmentId,
        DepartmentName = a.Department?.DeptName,
        Date = a.Date,
        ShiftType = a.ShiftType,
        ClockIn = a.ClockIn?.ToString("HH:mm"),
        ClockOut = a.ClockOut?.ToString("HH:mm"),
        Status = a.Status,
        LateFlag = a.LateFlag,
        LateMinutes = a.LateMinutes,
        EarlyLeaveFlag = a.EarlyLeaveFlag,
        EarlyLeaveMinutes = a.EarlyLeaveMinutes,
        Ot1Hours = a.Ot1Hours,
        Ot2Hours = a.Ot2Hours,
        Ot3Hours = a.Ot3Hours,
        ApprovalStatus = a.ApprovalStatus,
        Notes = a.Notes
    };
}
