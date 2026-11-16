using System.Net;
using System.Text;
using System.Text.Json;
using ErpBackend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class KeycloakSupplierIdentityClientTests
{
    [Fact]
    public async Task CreateOrFindInvitedUserAsync_CreatesLinkedIdentityAndHonorsOptionalTotp()
    {
        var supplierUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var actionsEmailBody = string.Empty;
        var createUserBody = string.Empty;
        var handler = new StubHttpMessageHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token"))
                return Json(HttpStatusCode.OK, """{"access_token":"admin-token","expires_in":300}""");

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/users"))
                return Json(HttpStatusCode.OK, "[]");

            if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/users"))
            {
                createUserBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Headers = { Location = new Uri("https://keycloak.example/realms/suppliers/users/subject-123") }
                };
            }

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/users/subject-123"))
                return Json(HttpStatusCode.OK, $$"""
                    {
                      "id":"subject-123",
                      "attributes":{
                        "erp_invitation_id":["{{correlationId}}"],
                        "erp_supplier_user_id":["{{supplierUserId}}"]
                      }
                    }
                    """);

            if (request.Method == HttpMethod.Put
                && request.RequestUri.AbsolutePath.EndsWith("/execute-actions-email"))
            {
                actionsEmailBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            throw new InvalidOperationException($"Unexpected Keycloak request: {request.Method} {request.RequestUri}");
        });
        var client = CreateClient(handler, requireTotp: false);

        var subject = await client.CreateOrFindInvitedUserAsync(
            "vendor@example.com",
            "Vendor Contact",
            correlationId,
            supplierUserId,
            CancellationToken.None);

        Assert.Equal("subject-123", subject);
        using var createdUser = JsonDocument.Parse(createUserBody);
        Assert.Equal("vendor@example.com", createdUser.RootElement.GetProperty("email").GetString());
        Assert.False(createdUser.RootElement.GetProperty("emailVerified").GetBoolean());
        Assert.Equal(
            new[] { "VERIFY_EMAIL", "UPDATE_PASSWORD" },
            createdUser.RootElement.GetProperty("requiredActions").EnumerateArray()
                .Select(action => action.GetString()).ToArray());
        Assert.Equal(
            new[] { "VERIFY_EMAIL", "UPDATE_PASSWORD" },
            JsonSerializer.Deserialize<string[]>(actionsEmailBody));
    }

    [Fact]
    public async Task CreateOrFindInvitedUserAsync_DoesNotBindExistingUnrelatedKeycloakUser()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token"))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"access_token":"admin-token","expires_in":300}"""));

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/users"))
                return Task.FromResult(Json(HttpStatusCode.OK, """[{"id":"existing-subject","attributes":{}}]"""));

            throw new InvalidOperationException($"Unexpected Keycloak request: {request.Method} {request.RequestUri}");
        });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<KeycloakSupplierIdentityException>(() =>
            client.CreateOrFindInvitedUserAsync(
                "existing@example.com",
                "Vendor Contact",
                Guid.NewGuid(),
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Equal("ExistingIdentityNotLinked", exception.ErrorCode);
    }

    [Fact]
    public async Task CreateOrFindInvitedUserAsync_ReusesOnlyIdentityWithMatchingInvitationCorrelation()
    {
        var supplierUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var createAttempted = false;
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token"))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"access_token":"admin-token","expires_in":300}"""));

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/users"))
                return Task.FromResult(Json(HttpStatusCode.OK, $$"""
                    [{
                      "id":"subject-reused",
                      "attributes":{
                        "erp_invitation_id":["{{correlationId}}"],
                        "erp_supplier_user_id":["{{supplierUserId}}"]
                      }
                    }]
                    """));

            if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/users"))
                createAttempted = true;

            if (request.Method == HttpMethod.Put
                && request.RequestUri.AbsolutePath.EndsWith("/execute-actions-email"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));

            throw new InvalidOperationException($"Unexpected Keycloak request: {request.Method} {request.RequestUri}");
        });
        var client = CreateClient(handler);

        var subject = await client.CreateOrFindInvitedUserAsync(
            "vendor@example.com",
            "Vendor Contact",
            correlationId,
            supplierUserId,
            CancellationToken.None);

        Assert.Equal("subject-reused", subject);
        Assert.False(createAttempted);
    }

    [Fact]
    public void ValidateConfiguration_AllowsLoopbackHttpOnlyInDevelopmentWhenEnabled()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => throw new InvalidOperationException("No requests expected.")),
            authority: "http://localhost:8182/realms/supplier-e2e",
            adminBaseUrl: "http://localhost:8182/admin/realms/supplier-e2e",
            allowHttpMetadata: true,
            environmentName: Environments.Development);

        client.ValidateConfiguration();
    }

    [Fact]
    public void ValidateConfiguration_RejectsLoopbackHttpOutsideDevelopment()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => throw new InvalidOperationException("No requests expected.")),
            authority: "http://localhost:8182/realms/supplier-e2e",
            adminBaseUrl: "http://localhost:8182/admin/realms/supplier-e2e",
            allowHttpMetadata: true,
            environmentName: Environments.Production);

        Assert.Throws<KeycloakSupplierIdentityException>(client.ValidateConfiguration);
    }

    private static KeycloakSupplierIdentityClient CreateClient(
        HttpMessageHandler handler,
        bool requireTotp = true,
        string authority = "https://keycloak.example/realms/suppliers",
        string adminBaseUrl = "https://keycloak.example/admin/realms/suppliers",
        bool allowHttpMetadata = false,
        string environmentName = "Production")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SupplierIdentity:Authority"] = authority,
                ["SupplierIdentity:AdminBaseUrl"] = adminBaseUrl,
                ["SupplierIdentity:AdminClientId"] = "erp-admin",
                ["SupplierIdentity:AdminClientSecret"] = "test-secret",
                ["SupplierIdentity:RequireTotp"] = requireTotp.ToString(),
                ["SupplierIdentity:AllowHttpMetadata"] = allowHttpMetadata.ToString()
            })
            .Build();
        return new KeycloakSupplierIdentityClient(
            new HttpClient(handler),
            configuration,
            new TestHostEnvironment(environmentName),
            NullLogger<KeycloakSupplierIdentityClient>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(KeycloakSupplierIdentityClientTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
