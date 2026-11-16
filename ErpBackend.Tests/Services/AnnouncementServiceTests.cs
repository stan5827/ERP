using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task GetAnnouncementsAsync_ShowsDepartmentAndCustomAnnouncementsOnlyToRecipients()
    {
        await using var db = CreateDbContext();
        var department = new Department { DeptCode = "D-01", DeptName = "Sales" };
        var recipient = CreateEmployee("E-001", "Recipient", department);
        var otherEmployee = CreateEmployee("E-002", "Other", department);
        var recipientUser = new User { Username = "recipient", PasswordHash = "hash", EmployeeId = recipient.Id };
        var otherUser = new User { Username = "other", PasswordHash = "hash", EmployeeId = otherEmployee.Id };
        db.Departments.Add(department);
        db.Employees.AddRange(recipient, otherEmployee);
        db.Users.AddRange(recipientUser, otherUser);
        await db.SaveChangesAsync();

        var service = new AnnouncementService(db);
        await service.CreateAnnouncementAsync(new CreateAnnouncementRequestDto
        {
            Title = "Department notice",
            TargetAudience = "department",
            PublishDeptId = department.Id,
            PublishDate = DateTime.UtcNow
        }, Guid.NewGuid());
        await service.CreateAnnouncementAsync(new CreateAnnouncementRequestDto
        {
            Title = "Selected recipients",
            TargetAudience = "custom",
            RecipientEmployeeIds = [recipient.Id],
            PublishDate = DateTime.UtcNow
        }, Guid.NewGuid());

        var recipientAnnouncements = await service.GetAnnouncementsAsync(recipientUser.Id);
        var otherAnnouncements = await service.GetAnnouncementsAsync(otherUser.Id);

        Assert.Equal(2, recipientAnnouncements.Count);
        Assert.Single(otherAnnouncements);
        Assert.Equal("Department notice", otherAnnouncements[0].Title);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_RequiresExplicitTargetsForDepartmentAndCustomAudiences()
    {
        await using var db = CreateDbContext();
        var service = new AnnouncementService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAnnouncementAsync(
            new CreateAnnouncementRequestDto
            {
                Title = "Missing department",
                TargetAudience = "department",
                PublishDate = DateTime.UtcNow
            },
            Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAnnouncementAsync(
            new CreateAnnouncementRequestDto
            {
                Title = "Missing recipients",
                TargetAudience = "custom",
                PublishDate = DateTime.UtcNow
            },
            Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAnnouncementsAsync_ShowsAuthoredCustomAnnouncementToPublisherWithoutEmployeeLink()
    {
        await using var db = CreateDbContext();
        var department = new Department { DeptCode = "D-02", DeptName = "HR" };
        var recipient = CreateEmployee("E-003", "Recipient", department);
        var publisher = new User
        {
            Username = "admin",
            PasswordHash = "hash",
            EmployeeId = null
        };
        var otherUser = new User { Username = "other-admin", PasswordHash = "hash" };
        db.Departments.Add(department);
        db.Employees.Add(recipient);
        db.Users.AddRange(publisher, otherUser);
        await db.SaveChangesAsync();

        var service = new AnnouncementService(db);
        await service.CreateAnnouncementAsync(new CreateAnnouncementRequestDto
        {
            Title = "Custom announcement",
            TargetAudience = "custom",
            RecipientEmployeeIds = [recipient.Id],
            PublishDate = DateTime.UtcNow
        }, publisher.Id);

        var publisherAnnouncements = await service.GetAnnouncementsAsync(publisher.Id);
        var otherAnnouncements = await service.GetAnnouncementsAsync(otherUser.Id);

        var publishedAnnouncement = Assert.Single(publisherAnnouncements);
        Assert.Equal("Custom announcement", publishedAnnouncement.Title);
        Assert.Empty(otherAnnouncements);
    }

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Employee CreateEmployee(string employeeNo, string name, Department department) =>
        new()
        {
            EmployeeNo = employeeNo,
            ChineseName = name,
            DepartmentId = department.Id,
            Department = department,
            Status = "active"
        };
}
