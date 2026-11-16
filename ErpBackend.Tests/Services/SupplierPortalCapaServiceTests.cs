using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.QA;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalCapaServiceTests
{
    [Fact]
    public async Task GetComplaintsAsync_ReturnsOnlyComplaintsForSupplier()
    {
        await using var db = CreateDbContext();
        var (supplierId, _) = SeedComplaint(db, "SUP-A");
        SeedComplaint(db, "SUP-B");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);

        var complaints = await service.GetComplaintsAsync(supplierId, CancellationToken.None);

        var complaint = Assert.Single(complaints);
        Assert.Equal("SUP-A-COMP-1", complaint.ComplaintNumber);
    }

    [Fact]
    public async Task GetComplaintAsync_HidesAnotherSuppliersComplaint()
    {
        await using var db = CreateDbContext();
        var (supplierId, complaint) = SeedComplaint(db, "SUP-A");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);

        Assert.Null(await service.GetComplaintAsync(complaint.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.NotNull(await service.GetComplaintAsync(complaint.Id, supplierId, CancellationToken.None));
    }

    [Fact]
    public async Task SaveDraftAsync_CreatesCapaAndPreservesPreviousRevisions()
    {
        await using var db = CreateDbContext();
        var (supplierId, complaint) = SeedComplaint(db, "SUP-A");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);

        var first = await service.SaveDraftAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "sales",
            new SupplierPortalCapaResponseRequestDto
            {
                D1Team = "Quality team",
                D2ProblemDescription = "Part dimension is out of spec"
            },
            CancellationToken.None);
        var second = await service.SaveDraftAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "sales",
            new SupplierPortalCapaResponseRequestDto { D2ProblemDescription = "Updated description" },
            CancellationToken.None);

        var capa = await db.SupplierCapas.SingleAsync();
        var revisions = await db.SupplierCapaResponseRevisions
            .OrderBy(revision => revision.RevisionNumber)
            .ToListAsync();
        Assert.Equal(first.CapaId, second.CapaId);
        Assert.Equal("in_progress", second.Status);
        Assert.Equal(2, second.RevisionNumber);
        Assert.Equal("Quality team", capa.D1_Team);
        Assert.Equal("Updated description", capa.D2_ProblemDescription);
        Assert.Equal("Part dimension is out of spec", revisions[0].D2_ProblemDescription);
        Assert.Equal("Updated description", revisions[1].D2_ProblemDescription);
        Assert.All(revisions, revision => Assert.Equal("draft_saved", revision.Action));
    }

    [Fact]
    public async Task SubmitResponseAsync_RequiresAllEightDisciplines()
    {
        await using var db = CreateDbContext();
        var (supplierId, complaint) = SeedComplaint(db, "SUP-A");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitResponseAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "sales",
            new SupplierPortalCapaResponseRequestDto { D1Team = "Team" },
            CancellationToken.None));

        Assert.Empty(await db.SupplierCapas.ToListAsync());
        Assert.Empty(await db.SupplierCapaResponseRevisions.ToListAsync());
    }

    [Fact]
    public async Task SubmitResponseAsync_RecordsSubmissionAndLocksSupplierChanges()
    {
        await using var db = CreateDbContext();
        var (supplierId, complaint) = SeedComplaint(db, "SUP-A");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);
        var submittedAtBefore = DateTime.UtcNow;

        var result = await service.SubmitResponseAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "sales",
            CompleteResponse(),
            CancellationToken.None);

        var capa = await db.SupplierCapas.SingleAsync();
        var revision = await db.SupplierCapaResponseRevisions.SingleAsync();
        Assert.Equal("submitted", result.Status);
        Assert.Equal("submitted", revision.Action);
        Assert.Equal("root cause", revision.D4_RootCause);
        Assert.True(capa.SupplierRespondedAt >= submittedAtBefore);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDraftAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "sales",
            new SupplierPortalCapaResponseRequestDto { D1Team = "Late update" },
            CancellationToken.None));
    }

    [Fact]
    public async Task SaveDraftAsync_RejectsViewerRoleAndOtherSuppliers()
    {
        await using var db = CreateDbContext();
        var (supplierId, complaint) = SeedComplaint(db, "SUP-A");
        await db.SaveChangesAsync();
        var service = new SupplierPortalCapaService(db);
        var request = new SupplierPortalCapaResponseRequestDto { D1Team = "Team" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveDraftAsync(
            complaint.Id,
            supplierId,
            Guid.NewGuid(),
            "viewer",
            request,
            CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SaveDraftAsync(
            complaint.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sales",
            request,
            CancellationToken.None));

        Assert.Empty(await db.SupplierCapas.ToListAsync());
    }

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (Guid SupplierId, SupplierComplaint Complaint) SeedComplaint(
        ErpGeneralDbContext db,
        string supplierCode)
    {
        var supplierId = Guid.NewGuid();
        var complaint = new SupplierComplaint
        {
            ComplaintNumber = $"{supplierCode}-COMP-1",
            SupplierId = supplierId,
            DefectQty = 3,
            Ppm = 120,
            Priority = "high",
            Description = "Dimension defect",
            Status = "open"
        };
        db.SupplierComplaints.Add(complaint);
        return (supplierId, complaint);
    }

    private static SupplierPortalCapaResponseRequestDto CompleteResponse() =>
        new()
        {
            D1Team = "Quality team",
            D2ProblemDescription = "Problem description",
            D3InterimContainment = "Containment",
            D4RootCause = "root cause",
            D5PermanentAction = "Permanent action",
            D6Verification = "Verification",
            D7Prevention = "Prevention",
            D8Closure = "Closure"
        };
}
