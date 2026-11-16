using ErpBackend.Data;
using ErpBackend.Models.HR;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class EmployeeEssAttendanceTests
{
    [Fact]
    public async Task ClockIn_RejectsWhenGeofenceIsNotConfigured()
    {
        await using var db = CreateInMemoryDbContext();
        var employee = await AddEmployeeAsync(db);
        var service = new AttendanceRecordService(db, CreateConfiguration());

        var status = await service.ClockInForEmployeeAsync(employee.Id, 25.03, 121.56, CancellationToken.None);

        Assert.Equal("geofence_not_configured", status);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task ClockIn_RejectsLocationOutsideConfiguredRadius()
    {
        await using var db = CreateInMemoryDbContext();
        var employee = await AddEmployeeAsync(db);
        var service = new AttendanceRecordService(db, CreateConfiguration(
            ("AttendanceGeofence:Latitude", "25.03"),
            ("AttendanceGeofence:Longitude", "121.56"),
            ("AttendanceGeofence:RadiusMeters", "200")));

        var status = await service.ClockInForEmployeeAsync(employee.Id, 26.03, 122.56, CancellationToken.None);

        Assert.Equal("outside_geofence", status);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task ClockIn_UsesServerLocalDateAndStoresLocation()
    {
        await using var db = CreateInMemoryDbContext();
        var employee = await AddEmployeeAsync(db);
        var service = new AttendanceRecordService(db, CreateConfiguration(
            ("AttendanceGeofence:Latitude", "25.03"),
            ("AttendanceGeofence:Longitude", "121.56"),
            ("AttendanceGeofence:RadiusMeters", "200"),
            ("AttendanceGeofence:TimeZoneId", "Asia/Taipei")));

        var status = await service.ClockInForEmployeeAsync(employee.Id, 25.03, 121.56, CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        var taipeiNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"));
        Assert.Equal("ok", status);
        Assert.Equal(taipeiNow.Date, record.Date);
        Assert.Equal(DateTimeKind.Utc, record.Date.Kind);
        Assert.Equal("25.03,121.56", record.ClockInLocation);
        Assert.NotNull(record.ClockIn);
        Assert.Equal(DateTimeKind.Utc, record.ClockIn.Value.Kind);
        Assert.Equal(record.Status == "late", record.LateFlag);
        Assert.Contains(record.Status, new[] { "normal", "late" });
    }

    [Fact]
    public async Task ClockOut_UsesServerTimeAndStoresLocation()
    {
        await using var db = CreateInMemoryDbContext();
        var employee = await AddEmployeeAsync(db);
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"));
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Date = now.Date,
            ClockIn = now.DateTime.AddHours(8),
            Status = "normal"
        });
        await db.SaveChangesAsync();
        var service = new AttendanceRecordService(db, CreateConfiguration(
            ("AttendanceGeofence:Latitude", "25.03"),
            ("AttendanceGeofence:Longitude", "121.56"),
            ("AttendanceGeofence:RadiusMeters", "200"),
            ("AttendanceGeofence:TimeZoneId", "Asia/Taipei")));

        var status = await service.ClockOutForEmployeeAsync(employee.Id, 25.03, 121.56, CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal("ok", status);
        Assert.NotNull(record.ClockOut);
        Assert.Equal("25.03,121.56", record.ClockOutLocation);
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static async Task<Employee> AddEmployeeAsync(ErpGeneralDbContext db)
    {
        var employee = new Employee { ChineseName = "ESS Test", EmployeeNo = $"E-{Guid.NewGuid():N}" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        var settings = values.ToDictionary(value => value.Key, value => (string?)value.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
