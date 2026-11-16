using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace ErpBackend.Services;

public sealed class KeycloakSupplierIdentityClient(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<KeycloakSupplierIdentityClient> logger) : IKeycloakSupplierIdentityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public void ValidateConfiguration()
    {
        var authority = GetRequiredSetting("SupplierIdentity:Authority");
        var adminBaseUrl = GetRequiredSetting("SupplierIdentity:AdminBaseUrl");
        _ = GetRequiredSetting("SupplierIdentity:AdminClientId");
        _ = GetRequiredSetting("SupplierIdentity:AdminClientSecret");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
            || !Uri.TryCreate(adminBaseUrl, UriKind.Absolute, out var adminUri))
            throw new KeycloakSupplierIdentityException("ProviderHttpsRequired");

        var requiresHttps =
            string.Equals(authorityUri!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && string.Equals(adminUri!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var allowsDevelopmentLoopbackHttp = environment.IsDevelopment()
            && configuration.GetValue<bool>("SupplierIdentity:AllowHttpMetadata")
            && string.Equals(authorityUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && string.Equals(adminUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && authorityUri.IsLoopback
            && adminUri.IsLoopback;
        if (!requiresHttps && !allowsDevelopmentLoopbackHttp)
            throw new KeycloakSupplierIdentityException("ProviderHttpsRequired");

        var invitationLifespan = configuration.GetValue("SupplierIdentity:InvitationLifespanSeconds", 86400);
        if (invitationLifespan is < 300 or > 604800)
            throw new KeycloakSupplierIdentityException("InvalidInvitationLifespan");
    }

    public async Task<string> CreateOrFindInvitedUserAsync(
        string email,
        string contactName,
        Guid invitationCorrelationId,
        Guid supplierUserId,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var existing = await FindUserByUsernameAsync(email, token, cancellationToken);
        string subject;

        if (existing is not null)
        {
            EnsureInvitationMatch(existing.Value, invitationCorrelationId, supplierUserId);
            subject = existing.Value.GetProperty("id").GetString()
                ?? throw new KeycloakSupplierIdentityException("InvalidProviderResponse");
        }
        else
        {
            subject = await CreateUserAsync(
                email,
                contactName,
                invitationCorrelationId,
                supplierUserId,
                token,
                cancellationToken);
        }

        await SendRequiredActionsEmailAsync(subject, token, cancellationToken);
        return subject;
    }

    public async Task ResendInvitationAsync(string subject, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        await SendRequiredActionsEmailAsync(subject, token, cancellationToken);
    }

    public async Task DisableUserAsync(string subject, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var user = await GetUserAsync(subject, token, cancellationToken);
        var update = new Dictionary<string, object?>
        {
            ["id"] = subject,
            ["enabled"] = false,
            ["username"] = user.GetProperty("username").GetString(),
            ["email"] = user.TryGetProperty("email", out var email) ? email.GetString() : null,
            ["emailVerified"] = user.TryGetProperty("emailVerified", out var verified) && verified.GetBoolean(),
            ["firstName"] = user.TryGetProperty("firstName", out var firstName) ? firstName.GetString() : null,
            ["lastName"] = user.TryGetProperty("lastName", out var lastName) ? lastName.GetString() : null,
            ["attributes"] = user.TryGetProperty("attributes", out var attributes)
                ? JsonSerializer.Deserialize<object>(attributes.GetRawText(), JsonOptions)
                : null,
            ["requiredActions"] = Array.Empty<string>()
        };

        await SendAdminRequestAsync(
            HttpMethod.Put,
            $"users/{Uri.EscapeDataString(subject)}",
            token,
            JsonContent(update),
            cancellationToken);
        await SendAdminRequestAsync(
            HttpMethod.Post,
            $"users/{Uri.EscapeDataString(subject)}/logout",
            token,
            null,
            cancellationToken);
    }

    public async Task<KeycloakSupplierIdentityStatus> GetUserStatusAsync(
        string subject,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var user = await GetUserAsync(subject, token, cancellationToken);
        var requiredActions = user.TryGetProperty("requiredActions", out var actions)
            && actions.ValueKind == JsonValueKind.Array
            && actions.GetArrayLength() > 0;
        Guid? correlationId = TryGetAttribute(user, "erp_invitation_id", out var correlationValue)
            && Guid.TryParse(correlationValue, out var parsedCorrelation)
                ? parsedCorrelation
                : null;

        using var credentialsResponse = await SendAdminRequestAsync(
            HttpMethod.Get,
            $"users/{Uri.EscapeDataString(subject)}/credentials",
            token,
            null,
            cancellationToken);
        using var credentialsDocument = await JsonDocument.ParseAsync(
            await credentialsResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        var hasTotp = credentialsDocument.RootElement.ValueKind == JsonValueKind.Array
            && credentialsDocument.RootElement.EnumerateArray().Any(credential =>
                credential.TryGetProperty("type", out var type)
                && string.Equals(type.GetString(), "otp", StringComparison.OrdinalIgnoreCase));

        return new KeycloakSupplierIdentityStatus(
            user.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean(),
            user.TryGetProperty("emailVerified", out var emailVerified) && emailVerified.GetBoolean(),
            requiredActions,
            hasTotp,
            correlationId);
    }

    private async Task<string> CreateUserAsync(
        string email,
        string contactName,
        Guid invitationCorrelationId,
        Guid supplierUserId,
        string token,
        CancellationToken cancellationToken)
    {
        var names = contactName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var user = new
        {
            username = email,
            email,
            firstName = names.FirstOrDefault(),
            lastName = names.Length > 1 ? names[1] : null,
            enabled = true,
            emailVerified = false,
            requiredActions = GetRequiredActions(),
            attributes = new Dictionary<string, string[]>
            {
                ["erp_invitation_id"] = [invitationCorrelationId.ToString()],
                ["erp_supplier_user_id"] = [supplierUserId.ToString()]
            }
        };

        using var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "users",
            token,
            JsonContent(user),
            cancellationToken,
            allowConflict: true);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var existing = await FindUserByUsernameAsync(email, token, cancellationToken);
            if (existing is null)
                throw new KeycloakSupplierIdentityException("UsernameConflict");
            EnsureInvitationMatch(existing.Value, invitationCorrelationId, supplierUserId);
            return existing.Value.GetProperty("id").GetString()
                ?? throw new KeycloakSupplierIdentityException("InvalidProviderResponse");
        }

        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(location))
            throw new KeycloakSupplierIdentityException("InvalidProviderResponse");
        var subject = Uri.UnescapeDataString(location.TrimEnd('/').Split('/').Last());
        var createdUser = await GetUserAsync(subject, token, cancellationToken);
        EnsureInvitationMatch(createdUser, invitationCorrelationId, supplierUserId);
        return subject;
    }

    private async Task<JsonElement?> FindUserByUsernameAsync(
        string username,
        string token,
        CancellationToken cancellationToken)
    {
        var url = QueryHelpers.AddQueryString("users", new Dictionary<string, string?>
        {
            ["username"] = username,
            ["exact"] = "true"
        });
        using var response = await SendAdminRequestAsync(
            HttpMethod.Get,
            url,
            token,
            null,
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        var matches = document.RootElement.EnumerateArray().ToArray();
        if (matches.Length > 1)
            throw new KeycloakSupplierIdentityException("AmbiguousProviderUser");
        return matches.Length == 0 ? null : matches[0].Clone();
    }

    private async Task<JsonElement> GetUserAsync(
        string subject,
        string token,
        CancellationToken cancellationToken)
    {
        using var response = await SendAdminRequestAsync(
            HttpMethod.Get,
            $"users/{Uri.EscapeDataString(subject)}",
            token,
            null,
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private async Task SendRequiredActionsEmailAsync(
        string subject,
        string token,
        CancellationToken cancellationToken)
    {
        var lifespan = configuration.GetValue("SupplierIdentity:InvitationLifespanSeconds", 86400);
        if (lifespan is < 300 or > 604800)
            throw new InvalidOperationException("Supplier invitation lifespan must be between 300 and 604800 seconds.");

        await SendAdminRequestAsync(
            HttpMethod.Put,
            $"users/{Uri.EscapeDataString(subject)}/execute-actions-email?lifespan={lifespan}",
            token,
            JsonContent(GetRequiredActions()),
            cancellationToken);
    }

    private string[] GetRequiredActions()
    {
        var actions = new List<string> { "VERIFY_EMAIL", "UPDATE_PASSWORD" };
        if (configuration.GetValue("SupplierIdentity:RequireTotp", true))
            actions.Add("CONFIGURE_TOTP");
        return actions.ToArray();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        if (!string.IsNullOrWhiteSpace(_accessToken)
            && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
                return _accessToken;

            var authority = GetRequiredSetting("SupplierIdentity:Authority").TrimEnd('/');
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{authority}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = GetRequiredSetting("SupplierIdentity:AdminClientId"),
                    ["client_secret"] = GetRequiredSetting("SupplierIdentity:AdminClientSecret")
                })
            };
            using var response = await SendTokenRequestAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new KeycloakSupplierIdentityException("AdminAuthenticationFailed");
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            _accessToken = document.RootElement.GetProperty("access_token").GetString();
            var expiresIn = document.RootElement.GetProperty("expires_in").GetInt32();
            if (string.IsNullOrWhiteSpace(_accessToken) || expiresIn <= 0)
                throw new KeycloakSupplierIdentityException("InvalidProviderResponse");
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return _accessToken;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not authenticate to the supplier identity provider.");
            throw new KeycloakSupplierIdentityException("AdminAuthenticationUnavailable");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Supplier identity provider returned an invalid token response.");
            throw new KeycloakSupplierIdentityException("InvalidProviderResponse");
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAdminRequestAsync(
        HttpMethod method,
        string relativePath,
        string token,
        HttpContent? content,
        CancellationToken cancellationToken,
        bool allowConflict = false)
    {
        var baseUrl = GetRequiredSetting("SupplierIdentity:AdminBaseUrl").TrimEnd('/');
        using var request = new HttpRequestMessage(
            method,
            $"{baseUrl}/{relativePath.TrimStart('/')}")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Supplier identity provider request failed.");
            throw new KeycloakSupplierIdentityException("ProviderUnavailable");
        }

        if (response.IsSuccessStatusCode
            || allowConflict && response.StatusCode == HttpStatusCode.Conflict)
            return response;

        var status = response.StatusCode;
        response.Dispose();
        throw new KeycloakSupplierIdentityException(status switch
        {
            HttpStatusCode.NotFound => "ProviderUserNotFound",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "AdminAuthorizationFailed",
            _ => "ProviderRequestFailed"
        });
    }

    private async Task<HttpResponseMessage> SendTokenRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not authenticate to the supplier identity provider.");
            throw new KeycloakSupplierIdentityException("AdminAuthenticationUnavailable");
        }
    }

    private static void EnsureInvitationMatch(
        JsonElement user,
        Guid invitationCorrelationId,
        Guid supplierUserId)
    {
        if (!TryGetAttribute(user, "erp_invitation_id", out var remoteInvitationId)
            || !string.Equals(
                remoteInvitationId,
                invitationCorrelationId.ToString(),
                StringComparison.OrdinalIgnoreCase)
            || !TryGetAttribute(user, "erp_supplier_user_id", out var remoteUserId)
            || !string.Equals(
                remoteUserId,
                supplierUserId.ToString(),
                StringComparison.OrdinalIgnoreCase))
            throw new KeycloakSupplierIdentityException("ExistingIdentityNotLinked");
    }

    private static bool TryGetAttribute(JsonElement user, string name, out string value)
    {
        value = string.Empty;
        if (!user.TryGetProperty("attributes", out var attributes)
            || !attributes.TryGetProperty(name, out var attribute)
            || attribute.ValueKind != JsonValueKind.Array
            || attribute.GetArrayLength() == 0)
            return false;

        value = attribute[0].GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private string GetRequiredSetting(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new KeycloakSupplierIdentityException("ProviderNotConfigured");

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");
}

public sealed class KeycloakSupplierIdentityException(string errorCode) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;
}
