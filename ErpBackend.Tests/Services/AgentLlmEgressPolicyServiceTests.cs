using System.Security.Claims;
using System.Text.Json;
using ErpBackend.Configs;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class AgentLlmEgressPolicyServiceTests
{
    [Fact]
    public async Task EvaluateAndAuditAsync_BlocksPublicEndpointAndAuditsWithoutLoggingEndpoint()
    {
        using var fixture = Fixture.Create(enabled: true);
        Assert.Equal(
            fixture.UserId.ToString(),
            fixture.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var decision = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "openai",
                "gpt-4",
                "https://api.openai.com/v1",
                "internal"));

        Assert.False(decision.Allowed);
        Assert.Equal("destination_not_loopback", decision.ReasonCode);
        var disclosure = Assert.Single(await fixture.Db.DisclosureLogs.ToListAsync());
        Assert.Equal("egress_blocked", disclosure.Outcome);
        Assert.Equal("agent.llm.egress.v1", disclosure.ToolName);
        Assert.Equal("llm-egress-v1", disclosure.PolicyVersion);
        Assert.Equal(fixture.UserId, disclosure.ActorUserId);
        Assert.DoesNotContain("api.openai.com", disclosure.DecisionReason);
        Assert.Single(await fixture.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task EvaluateAndAuditAsync_RequiresExplicitProviderModelAndLoopbackEndpointAllowlist()
    {
        using var fixture = Fixture.Create(
            enabled: true,
            approvedDestinations:
            [
                new AgentLlmDestinationOptions
                {
                    Provider = "ollama",
                    Model = "qwen2.5:7b",
                    Endpoint = "http://localhost:11434"
                }
            ]);

        var allowed = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "qwen2.5:7b",
                "http://localhost:11434",
                "internal"));
        var wrongModel = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "unlisted-model",
                "http://localhost:11434",
                "internal"));
        var nonLoopback = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "qwen2.5:7b",
                "http://localhost.example:11434",
                "internal"));
        var wrongClassification = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "qwen2.5:7b",
                "http://localhost:11434",
                "confidential"));
        var credentials = await fixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "qwen2.5:7b",
                "http://localhost:11434",
                "internal",
                HasProviderCredentials: true));

        Assert.True(allowed.Allowed);
        Assert.Equal("approved_for_local_provider", allowed.ReasonCode);
        Assert.False(wrongModel.Allowed);
        Assert.Equal("provider_model_or_endpoint_not_allowlisted", wrongModel.ReasonCode);
        Assert.False(nonLoopback.Allowed);
        Assert.Equal("destination_not_loopback", nonLoopback.ReasonCode);
        Assert.False(wrongClassification.Allowed);
        Assert.Equal("data_classification_not_approved", wrongClassification.ReasonCode);
        Assert.False(credentials.Allowed);
        Assert.Equal("provider_credentials_not_allowed", credentials.ReasonCode);

        var disclosures = await fixture.Db.DisclosureLogs.OrderBy(log => log.CreatedAt).ToListAsync();
        Assert.Equal(5, disclosures.Count);
        Assert.Equal("egress_approved", disclosures[0].Outcome);
        Assert.All(disclosures.Skip(1), item => Assert.Equal("egress_blocked", item.Outcome));
    }

    [Fact]
    public async Task EvaluateAndAuditAsync_FailsClosedWhenFeatureIsDisabledOrAuditFails()
    {
        using var disabledFixture = Fixture.Create(
            enabled: false,
            approvedDestinations:
            [
                new AgentLlmDestinationOptions
                {
                    Provider = "ollama",
                    Model = "qwen2.5:7b",
                    Endpoint = "http://localhost:11434"
                }
            ]);

        var disabledDecision = await disabledFixture.Policy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                "ollama",
                "qwen2.5:7b",
                "http://localhost:11434",
                "internal"));

        Assert.False(disabledDecision.Allowed);
        Assert.Equal("egress_feature_disabled", disabledDecision.ReasonCode);
        Assert.Equal("egress_blocked", (await disabledFixture.Db.DisclosureLogs.SingleAsync()).Outcome);

        using var failedAuditFixture = Fixture.Create(
            enabled: false,
            failAudit: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failedAuditFixture.Policy.EvaluateAndAuditAsync(
                new AgentLlmEgressRequestDto(
                    "ollama",
                    "qwen2.5:7b",
                    "http://localhost:11434",
                    "internal")));
    }

    [Fact]
    public async Task PolicyGuardedLlmService_DoesNotCallProviderWhenEgressIsDenied()
    {
        using var fixture = Fixture.Create(enabled: false);
        var inner = new CountingLlmService();
        var guarded = new PolicyGuardedLlmService(
            inner,
            fixture.Policy,
            new LlmConfig
            {
                Provider = "openai",
                BaseUrl = "https://api.openai.com/v1",
                DefaultModel = "gpt-4"
            });

        var response = await guarded.CompleteAsync(new LlmRequest
        {
            Prompt = "synthetic prompt",
            Model = "gpt-4"
        });

        Assert.False(response.Success);
        Assert.Equal("LLM request blocked by the server egress policy.", response.Error);
        Assert.Equal(0, inner.CompletionCount);
        Assert.Equal("egress_blocked", (await fixture.Db.DisclosureLogs.SingleAsync()).Outcome);
    }

    [Fact]
    public async Task PolicyGuardedLlmService_DelegatesOnlyAfterApprovedLocalPolicyAndAudit()
    {
        using var fixture = Fixture.Create(
            enabled: true,
            approvedDestinations:
            [
                new AgentLlmDestinationOptions
                {
                    Provider = "ollama",
                    Model = "qwen2.5:7b",
                    Endpoint = "http://localhost:11434"
                }
            ]);
        var inner = new CountingLlmService();
        var guarded = new PolicyGuardedLlmService(
            inner,
            fixture.Policy,
            new LlmConfig
            {
                Provider = "ollama",
                BaseUrl = "http://localhost:11434",
                DefaultModel = "qwen2.5:7b"
            });

        var response = await guarded.CompleteAsync(new LlmRequest
        {
            Prompt = "synthetic prompt",
            Model = "qwen2.5:7b",
            DataClassification = "internal"
        });

        Assert.True(response.Success);
        Assert.Equal(1, inner.CompletionCount);
        Assert.Equal("egress_approved", (await fixture.Db.DisclosureLogs.SingleAsync()).Outcome);
    }

    [Fact]
    public void NumericFacts_ExposeDeterministicDisplayValuesWithUnitKeptSeparate()
    {
        var fact = new AgentWipDecimalFactDto(
            "wo/quantity",
            123456789.123456789m,
            "WipReports",
            "CompletedQty",
            null,
            "KG");
        var notApplicableFact = new AgentWipNullableDecimalFactDto(
            "wo/defect-rate",
            null,
            "WipReports",
            "DefectRate",
            null,
            "%",
            "zero_denominator");

        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("123456789.123456789", fact.DisplayValue);
            Assert.Equal("KG", fact.Unit);
            Assert.Null(notApplicableFact.DisplayValue);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }

        var json = JsonSerializer.Serialize(
            fact,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"displayValue\":\"123456789.123456789\"", json);
        Assert.Contains("\"unit\":\"KG\"", json);
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(
            ErpGeneralDbContext db,
            AgentLlmEgressPolicyService policy,
            Guid userId,
            HttpContext httpContext)
        {
            Db = db;
            Policy = policy;
            UserId = userId;
            HttpContext = httpContext;
        }

        public ErpGeneralDbContext Db { get; }
        public AgentLlmEgressPolicyService Policy { get; }
        public Guid UserId { get; }
        public HttpContext HttpContext { get; }

        public static Fixture Create(
            bool enabled,
            IReadOnlyList<AgentLlmDestinationOptions>? approvedDestinations = null,
            bool failAudit = false)
        {
            var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = failAudit
                ? new FailingAuditDbContext(options) { FailWrites = true }
                : new ErpGeneralDbContext(options);
            var userId = Guid.NewGuid();
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "test"))
            };
            context.TraceIdentifier = Guid.NewGuid().ToString("N");
            var accessor = new HttpContextAccessor { HttpContext = context };
            var audit = new AgentDisclosureAuditService(db, accessor);
            var policy = new AgentLlmEgressPolicyService(
                Options.Create(new AgentLlmEgressOptions
                {
                    Enabled = enabled,
                    ApprovedDestinations = approvedDestinations?.ToList() ?? []
                }),
                audit,
                accessor);

            return new Fixture(db, policy, userId, context);
        }

        public void Dispose() => Db.Dispose();
    }

    private sealed class FailingAuditDbContext(DbContextOptions<ErpGeneralDbContext> options)
        : ErpGeneralDbContext(options)
    {
        public bool FailWrites { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => FailWrites
                ? throw new InvalidOperationException("Synthetic audit failure.")
                : base.SaveChangesAsync(cancellationToken);
    }

    private sealed class CountingLlmService : ILlmService
    {
        public int CompletionCount { get; private set; }

        public Task<LlmResponse> CompleteAsync(LlmRequest request)
        {
            CompletionCount++;
            return Task.FromResult(new LlmResponse { Success = true });
        }

        public Task<string> GetModelNameAsync() => Task.FromResult("test-model");
    }
}
