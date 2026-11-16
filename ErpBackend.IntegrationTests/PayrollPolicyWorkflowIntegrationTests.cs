using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpBackend.Models.System;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class PayrollPolicyWorkflowIntegrationTests
{
    private static readonly CustomWebApplicationFactory Factory = new();

    [Fact]
    public async Task PolicyWorkflow_RequiresCompletePayloadAndIndependentFinanceReviewer_AndNeverEnablesCalculation()
    {
        var hrUserId = CustomWebApplicationFactory.DefaultTestUserId;
        var financeUserId = Guid.NewGuid();
        using (var db = Factory.CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = financeUserId,
                Username = "uat-finance-reviewer",
                PasswordHash = "unused"
            });
            db.SaveChanges();
        }

        using var client = CreateAdminClient(hrUserId);
        var request = new
        {
            versionCode = $"TEST-{Guid.NewGuid():N}",
            effectiveFrom = "2026-01-01",
            approvalDocumentReference = "UAT-HR-POLICY-TEST",
            policyJson = "{}"
        };

        var createResponse = await client.PostAsJsonAsync("/api/erp/payroll/policies", request);
        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created,
            $"Expected Created but received {(int)createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var policyId = created.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var incompleteResponse = await client.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/submit",
            new { confirmedOfficialSources = false });
        Assert.Equal(HttpStatusCode.Conflict, incompleteResponse.StatusCode);
        using (var incomplete = JsonDocument.Parse(await incompleteResponse.Content.ReadAsStringAsync()))
        {
            var missingFields = incomplete.RootElement.GetProperty("missingFields")
                .EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.Contains("overtimeRules", missingFields);
            Assert.Contains("insuranceTables", missingFields);
        }

        var completeRequest = new
        {
            request.versionCode,
            request.effectiveFrom,
            request.approvalDocumentReference,
            policyJson = """
                {
                  "overtimeRules": [{"dayType":"uat","fromHour":0,"toHour":1,"multiplier":1,"multiplierMode":"additional"}],
                  "insuranceTables": {
                    "laborInsurance":[{"grade":"UAT","monthlyBase":1,"employeeRate":0,"employerRate":0}],
                    "healthInsurance":[{"grade":"UAT","monthlyBase":1,"employeeRate":0,"employerRate":0}],
                    "employmentInsurance":[{"grade":"UAT","monthlyBase":1,"employeeRate":0,"employerRate":0}],
                    "occupationalAccidentInsurance":[{"grade":"UAT","monthlyBase":1,"employeeRate":0,"employerRate":0}]
                  },
                  "pensionRules": {"employerRate":0,"employeeMinRate":0,"employeeMaxRate":0},
                  "taxRules": {
                    "withholdingTable":[{"lowerBound":0,"rate":0}],
                    "supplementalHealthInsurance":{"threshold":0,"rate":0}
                  },
                  "leaveRules": {
                    "dailyDivisor":1,"hourlyDivisor":1,
                    "deductions":[{"leaveType":"uat","deductionRate":0}]
                  }
                }
                """
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/erp/payroll/policies/{policyId}", completeRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var unconfirmedSubmitResponse = await client.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/submit",
            new { confirmedOfficialSources = false });
        Assert.Equal(HttpStatusCode.Conflict, unconfirmedSubmitResponse.StatusCode);

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/submit",
            new { confirmedOfficialSources = true });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var sameActorApproval = await client.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/approve",
            new { comment = "Same person must not approve", confirmedOfficialSources = true });
        Assert.Equal(HttpStatusCode.Conflict, sameActorApproval.StatusCode);

        using var financeClient = CreateAdminClient(financeUserId);
        var unconfirmedApproval = await financeClient.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/approve",
            new { comment = "Synthetic UAT second reviewer", confirmedOfficialSources = false });
        Assert.Equal(HttpStatusCode.Conflict, unconfirmedApproval.StatusCode);

        var approveResponse = await financeClient.PostAsJsonAsync(
            $"/api/erp/payroll/policies/{policyId}/approve",
            new { comment = "Synthetic UAT second reviewer", confirmedOfficialSources = true });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        using (var approved = JsonDocument.Parse(await approveResponse.Content.ReadAsStringAsync()))
        {
            var data = approved.RootElement.GetProperty("data");
            Assert.Equal("approved", data.GetProperty("status").GetString());
            Assert.False(data.GetProperty("calculationEnabled").GetBoolean());
            Assert.Equal(hrUserId, data.GetProperty("submittedByUserId").GetGuid());
            Assert.Equal(financeUserId, data.GetProperty("financeReviewerUserId").GetGuid());
            Assert.Equal(4, data.GetProperty("auditTrail").GetArrayLength());
        }

        var immutableResponse = await client.PutAsJsonAsync($"/api/erp/payroll/policies/{policyId}", completeRequest);
        Assert.Equal(HttpStatusCode.Conflict, immutableResponse.StatusCode);

        var calculateResponse = await client.PostAsJsonAsync(
            "/api/erp/payroll/calculate",
            new { employeeId = Guid.NewGuid(), periodStart = "2026-01-01", periodEnd = "2026-01-31" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, calculateResponse.StatusCode);
    }

    [Fact]
    public async Task PolicyEndpoints_RequireModulePermissions()
    {
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/erp/payroll/policies");
        var financeResponse = await client.GetAsync("/api/erp/payroll/policies/pending-finance-review");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, financeResponse.StatusCode);
    }

    private static HttpClient CreateAdminClient(Guid userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        return client;
    }
}
