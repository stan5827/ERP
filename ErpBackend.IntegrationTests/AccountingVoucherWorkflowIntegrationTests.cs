using System.Net;
using System.Text;
using System.Text.Json;
using ErpBackend.Data;
using ErpBackend.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class AccountingVoucherWorkflowIntegrationTests
{
    private static readonly CustomWebApplicationFactory Factory = new();
    private readonly HttpClient _client = Factory.CreateClient();

    [Fact]
    public async Task ManualVoucher_CanBeSavedPostedAndQueriedInJournal()
    {
        Guid debitAccountId;
        Guid creditAccountId;
        using (var db = Factory.CreateDbContext())
        {
            var accounts = new[]
            {
                new GLAccount
                {
                    AccountCode = $"D{Guid.NewGuid():N}"[..20],
                    AccountName = "測試借方科目",
                    AccountType = "Asset"
                },
                new GLAccount
                {
                    AccountCode = $"C{Guid.NewGuid():N}"[..20],
                    AccountName = "測試貸方科目",
                    AccountType = "Revenue"
                }
            };
            db.GLAccounts.AddRange(accounts);
            await db.SaveChangesAsync();
            debitAccountId = accounts[0].Id;
            creditAccountId = accounts[1].Id;
        }

        var createResponse = await _client.PostAsync(
            "/api/erp/finance/vouchers",
            JsonContent(new
            {
                voucherDate = "2026-08-15",
                voucherType = "Journal",
                description = "整合測試人工傳票",
                entries = new[]
                {
                    new { glAccountId = debitAccountId, debitAmount = 125.50m, creditAmount = 0m },
                    new { glAccountId = creditAccountId, debitAmount = 0m, creditAmount = 125.50m }
                }
            }));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var created = createdDocument.RootElement.GetProperty("data");
        var voucherId = created.GetProperty("id").GetGuid();
        Assert.Equal("Draft", created.GetProperty("status").GetString());
        Assert.Equal(1, created.GetProperty("entries")[0].GetProperty("lineNumber").GetInt32());
        Assert.Equal(125.50m, created.GetProperty("debitTotal").GetDecimal());

        var draftUpdateResponse = await _client.PutAsync(
            $"/api/erp/finance/vouchers/{voucherId}",
            JsonContent(new
            {
                voucherDate = "2026-08-15",
                voucherType = "Journal",
                description = "更新後的測試傳票",
                entries = new[]
                {
                    new { glAccountId = debitAccountId, debitAmount = 125.50m, creditAmount = 0m, memo = "更新借方" },
                    new { glAccountId = creditAccountId, debitAmount = 0m, creditAmount = 125.50m, memo = "更新貸方" }
                }
            }));
        Assert.Equal(HttpStatusCode.OK, draftUpdateResponse.StatusCode);

        var submitResponse = await _client.PostAsync(
            $"/api/erp/finance/vouchers/{voucherId}/submit",
            content: null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        using var submittedDocument = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
        Assert.Equal("PendingApproval", submittedDocument.RootElement.GetProperty("data").GetProperty("status").GetString());

        var selfApprovalResponse = await _client.PostAsync(
            $"/api/erp/finance/vouchers/{voucherId}/approve",
            content: null);
        Assert.Equal(HttpStatusCode.Forbidden, selfApprovalResponse.StatusCode);

        using var approveRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/erp/finance/vouchers/{voucherId}/approve");
        approveRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        approveRequest.Headers.Add("X-Test-Roles", "Admin");
        var postResponse = await _client.SendAsync(approveRequest);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        using var postedDocument = JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
        Assert.Equal("Posted", postedDocument.RootElement.GetProperty("data").GetProperty("status").GetString());

        var journalResponse = await _client.GetAsync(
            $"/api/erp/finance/journals?fromDate=2026-08-15&toDate=2026-08-15&accountId={debitAccountId}");
        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);
        using var journalDocument = JsonDocument.Parse(await journalResponse.Content.ReadAsStringAsync());
        var journal = journalDocument.RootElement.GetProperty("data");
        Assert.Equal(1, journal.GetProperty("totalCount").GetInt32());
        Assert.Equal(125.50m, journal.GetProperty("debitTotal").GetDecimal());
        Assert.Equal(0m, journal.GetProperty("creditTotal").GetDecimal());
        Assert.Equal(created.GetProperty("voucherNumber").GetString(),
            journal.GetProperty("entries")[0].GetProperty("sourceDocNo").GetString());

        var updateResponse = await _client.PutAsync(
            $"/api/erp/finance/vouchers/{voucherId}",
            JsonContent(new
            {
                voucherDate = "2026-08-15",
                voucherType = "Journal",
                description = "不得修改",
                entries = new[]
                {
                    new { glAccountId = debitAccountId, debitAmount = 125.50m, creditAmount = 0m },
                    new { glAccountId = creditAccountId, debitAmount = 0m, creditAmount = 125.50m }
                }
            }));
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UnbalancedDraftCannotBeSubmittedForApproval()
    {
        Guid debitAccountId;
        Guid creditAccountId;
        using (var db = Factory.CreateDbContext())
        {
            var accounts = new[]
            {
                new GLAccount
                {
                    AccountCode = $"D{Guid.NewGuid():N}"[..20],
                    AccountName = "測試借方科目",
                    AccountType = "Asset"
                },
                new GLAccount
                {
                    AccountCode = $"C{Guid.NewGuid():N}"[..20],
                    AccountName = "測試貸方科目",
                    AccountType = "Revenue"
                }
            };
            db.GLAccounts.AddRange(accounts);
            await db.SaveChangesAsync();
            debitAccountId = accounts[0].Id;
            creditAccountId = accounts[1].Id;
        }

        var response = await _client.PostAsync(
            "/api/erp/finance/vouchers",
            JsonContent(new
            {
                voucherDate = "2026-08-16",
                voucherType = "Journal",
                entries = new[]
                {
                    new { glAccountId = debitAccountId, debitAmount = 100m, creditAmount = 0m },
                    new { glAccountId = creditAccountId, debitAmount = 0m, creditAmount = 90m }
                }
            }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var voucherId = document.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var postResponse = await _client.PostAsync(
            $"/api/erp/finance/vouchers/{voucherId}/submit",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        using var errorDocument = JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
        Assert.Contains("借貸不平衡", errorDocument.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task DraftInClosedPeriodCannotBeCreated()
    {
        var closedDate = new DateOnly(2026, 9, 10);
        Guid debitAccountId;
        Guid creditAccountId;
        using (var db = Factory.CreateDbContext())
        {
            var accounts = new[]
            {
                new GLAccount
                {
                    AccountCode = $"D{Guid.NewGuid():N}"[..20],
                    AccountName = "測試借方科目",
                    AccountType = "Asset"
                },
                new GLAccount
                {
                    AccountCode = $"C{Guid.NewGuid():N}"[..20],
                    AccountName = "測試貸方科目",
                    AccountType = "Revenue"
                }
            };
            db.GLAccounts.AddRange(accounts);
            db.AccountingPeriods.Add(new AccountingPeriod
            {
                PeriodCode = $"C-{Guid.NewGuid():N}"[..30],
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2026, 9, 30),
                IsClosed = true,
                ClosedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            debitAccountId = accounts[0].Id;
            creditAccountId = accounts[1].Id;
        }

        var response = await _client.PostAsync(
            "/api/erp/finance/vouchers",
            JsonContent(new
            {
                voucherDate = closedDate.ToString("yyyy-MM-dd"),
                voucherType = "Journal",
                entries = new[]
                {
                    new { glAccountId = debitAccountId, debitAmount = 100m, creditAmount = 0m },
                    new { glAccountId = creditAccountId, debitAmount = 0m, creditAmount = 100m }
                }
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
