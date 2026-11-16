$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$localDirectory = Join-Path $PSScriptRoot '.local'
$secretFile = Join-Path $localDirectory 'main-erp-wip.env'
$composeFile = Join-Path $PSScriptRoot 'docker-compose.main-erp-wip.yml'
$settings = @{}

function New-RandomHex([int]$byteCount) {
    $bytes = New-Object byte[] $byteCount
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

if (!(Test-Path -LiteralPath $localDirectory)) {
    New-Item -ItemType Directory -Path $localDirectory | Out-Null
}

if (!(Test-Path -LiteralPath $secretFile)) {
    $secretSettings = [ordered]@{
        MAIN_ERP_WIP_POSTGRES_PASSWORD = New-RandomHex 32
        MAIN_ERP_WIP_JWT_SECRET = New-RandomHex 48
        MAIN_ERP_WIP_ADMIN_INITIAL_PASSWORD = New-RandomHex 18
        MAIN_ERP_WIP_KEYCLOAK_ADMIN_USERNAME = 'wip-admin'
        MAIN_ERP_WIP_KEYCLOAK_ADMIN_PASSWORD = New-RandomHex 24
        MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_ID = 'erp-main-wip-backend'
        MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_SECRET = New-RandomHex 32
    }
    $lines = foreach ($entry in $secretSettings.GetEnumerator()) {
        '{0}={1}' -f $entry.Key, $entry.Value
    }
    [IO.File]::WriteAllLines($secretFile, $lines, [Text.Encoding]::ASCII)
    Write-Output 'Created ignored local WIP credentials file.'
}

Get-Content -LiteralPath $secretFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        $settings[$matches[1].Trim()] = $matches[2]
    }
}

$requiredSettings = @(
    'MAIN_ERP_WIP_POSTGRES_PASSWORD',
    'MAIN_ERP_WIP_JWT_SECRET',
    'MAIN_ERP_WIP_ADMIN_INITIAL_PASSWORD',
    'MAIN_ERP_WIP_KEYCLOAK_ADMIN_USERNAME',
    'MAIN_ERP_WIP_KEYCLOAK_ADMIN_PASSWORD',
    'MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_ID',
    'MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_SECRET'
)
foreach ($key in $requiredSettings) {
    if ([string]::IsNullOrWhiteSpace($settings[$key])) {
        throw "Required local setting is missing: $key"
    }
}

$composeArgs = @(
    'compose',
    '--env-file', $secretFile,
    '-p', 'erp-main-wip',
    '-f', $composeFile,
    'up', '-d'
)
& docker @composeArgs
if ($LASTEXITCODE -ne 0) {
    throw 'Could not start the isolated ERP WIP PostgreSQL and Keycloak containers.'
}

$databaseReady = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    & docker exec erp-main-wip-postgres pg_isready -U erp -d ErpGeneral *> $null
    if ($LASTEXITCODE -eq 0) {
        $databaseReady = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (!$databaseReady) {
    throw 'The isolated WIP PostgreSQL container did not become ready.'
}

$keycloakBaseUrl = 'http://127.0.0.1:8183'
$keycloakReady = $false
for ($attempt = 0; $attempt -lt 90; $attempt++) {
    try {
        $null = Invoke-RestMethod -Uri "$keycloakBaseUrl/realms/master/.well-known/openid-configuration" -TimeoutSec 3
        $keycloakReady = $true
        break
    }
    catch {
        Start-Sleep -Seconds 2
    }
}
if (!$keycloakReady) {
    throw 'The isolated WIP Keycloak server did not become ready.'
}

$tokenResponse = Invoke-RestMethod -Method Post `
    -Uri "$keycloakBaseUrl/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{
        grant_type = 'password'
        client_id = 'admin-cli'
        username = $settings['MAIN_ERP_WIP_KEYCLOAK_ADMIN_USERNAME']
        password = $settings['MAIN_ERP_WIP_KEYCLOAK_ADMIN_PASSWORD']
    }
$adminHeaders = @{ Authorization = "Bearer $($tokenResponse.access_token)" }

function Invoke-KeycloakAdmin([string]$Method, [string]$Path, $Body = $null) {
    $parameters = @{
        Method = $Method
        Uri = "$keycloakBaseUrl$Path"
        Headers = $adminHeaders
        ErrorAction = 'Stop'
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = ConvertTo-Json -InputObject $Body -Depth 30
    }
    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        throw "Keycloak Admin API request $Method $Path failed: $($_.Exception.Message)"
    }
}

$realmName = 'erp-main-wip'
$existingRealms = Invoke-KeycloakAdmin 'Get' '/admin/realms'
$realmExists = @($existingRealms | Where-Object { $_.realm -eq $realmName }).Count -gt 0

if (!$realmExists) {
    $null = Invoke-KeycloakAdmin 'Post' '/admin/realms' @{
        realm = $realmName
        enabled = $true
        displayName = 'ERP Main WIP'
        registrationAllowed = $false
        loginWithEmailAllowed = $true
        duplicateEmailsAllowed = $false
        resetPasswordAllowed = $false
    }
}

function Get-KeycloakClient([string]$ClientId) {
    $encodedClientId = [Uri]::EscapeDataString($ClientId)
    $clients = Invoke-KeycloakAdmin 'Get' "/admin/realms/$realmName/clients?clientId=$encodedClientId"
    return @($clients | Where-Object { $_.clientId -eq $ClientId }) | Select-Object -First 1
}

$portalClientId = 'erp-main-wip-supplier-portal'
$portalClient = Get-KeycloakClient $portalClientId
if ($null -eq $portalClient) {
    $null = Invoke-KeycloakAdmin 'Post' "/admin/realms/$realmName/clients" @{
        clientId = $portalClientId
        name = 'ERP Main WIP Supplier Portal'
        enabled = $true
        protocol = 'openid-connect'
        publicClient = $true
        standardFlowEnabled = $true
        implicitFlowEnabled = $false
        directAccessGrantsEnabled = $false
        serviceAccountsEnabled = $false
        redirectUris = @(
            'http://localhost:5178/auth/callback',
            'http://localhost:5178/auth/logout-callback'
        )
        webOrigins = @('http://localhost:5178')
        attributes = @{
            'pkce.code.challenge.method' = 'S256'
            'post.logout.redirect.uris' = 'http://localhost:5178/auth/logout-callback'
        }
        protocolMappers = @(
            @{
                name = 'supplier-portal-api-audience'
                protocol = 'openid-connect'
                protocolMapper = 'oidc-audience-mapper'
                consentRequired = $false
                config = @{
                    'included.client.audience' = 'supplier-portal-api'
                    'id.token.claim' = 'false'
                    'access.token.claim' = 'true'
                    'introspection.token.claim' = 'true'
                }
            }
        )
    }
}

$serviceClientId = $settings['MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_ID']
$serviceClient = Get-KeycloakClient $serviceClientId
if ($null -eq $serviceClient) {
    $null = Invoke-KeycloakAdmin 'Post' "/admin/realms/$realmName/clients" @{
        clientId = $serviceClientId
        name = 'ERP Main WIP Backend'
        enabled = $true
        protocol = 'openid-connect'
        publicClient = $false
        secret = $settings['MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_SECRET']
        standardFlowEnabled = $false
        implicitFlowEnabled = $false
        directAccessGrantsEnabled = $false
        serviceAccountsEnabled = $true
    }
    $serviceClient = Get-KeycloakClient $serviceClientId
}

if ($null -eq $serviceClient) {
    throw 'The isolated WIP Keycloak backend service client was not created.'
}

$realmManagement = Get-KeycloakClient 'realm-management'
if ($null -eq $realmManagement) {
    throw 'The Keycloak realm-management client is missing.'
}
$serviceAccount = Invoke-KeycloakAdmin 'Get' "/admin/realms/$realmName/clients/$($serviceClient.id)/service-account-user"
$roles = Invoke-KeycloakAdmin 'Get' "/admin/realms/$realmName/clients/$($realmManagement.id)/roles"
$requiredRoleNames = @('view-users', 'query-users', 'manage-users')
$requiredRoles = @($roles | Where-Object { $requiredRoleNames -contains $_.name })
if ($requiredRoles.Count -ne $requiredRoleNames.Count) {
    throw 'Keycloak is missing one or more required realm-management roles.'
}
$currentRoles = Invoke-KeycloakAdmin 'Get' "/admin/realms/$realmName/users/$($serviceAccount.id)/role-mappings/clients/$($realmManagement.id)"
$missingRoles = @($requiredRoles | Where-Object { $_.name -notin @($currentRoles | ForEach-Object { $_.name }) })
if ($missingRoles.Count -gt 0) {
    $null = Invoke-KeycloakAdmin 'Post' "/admin/realms/$realmName/users/$($serviceAccount.id)/role-mappings/clients/$($realmManagement.id)" $missingRoles
}

$databaseTableCount = (& docker exec erp-main-wip-postgres psql -U erp -d ErpGeneral -At -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';").Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the isolated WIP database before migration.'
}
if ([int]$databaseTableCount -eq 0) {
    $migrationSql = Join-Path $localDirectory 'main-erp-wip-migrations.sql'
    Push-Location $repositoryRoot
    try {
        dotnet build 'src\ErpBackend\ErpBackend.csproj' --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw 'The backend build failed; WIP schema was not initialized.'
        }
        dotnet ef migrations script 0 `
            --configuration Release `
            --no-build `
            --project 'src\ErpBackend\ErpBackend.csproj' `
            --startup-project 'src\ErpBackend\ErpBackend.csproj' `
            --output $migrationSql
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not generate the isolated WIP database migration script.'
        }
    }
    finally {
        Pop-Location
    }

    $remoteSql = '/tmp/erp-main-wip-migrations.sql'
    & docker cp $migrationSql ("erp-main-wip-postgres:{0}" -f $remoteSql)
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not stage the generated migration script in the isolated WIP database container.'
    }
    & docker exec erp-main-wip-postgres psql -v ON_ERROR_STOP=1 -U erp -d ErpGeneral -f $remoteSql
    $migrationExitCode = $LASTEXITCODE
    & docker exec erp-main-wip-postgres rm -- $remoteSql
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not remove the exact staged WIP migration file.'
    }
    Remove-Item -LiteralPath $migrationSql -Force
    if ($migrationExitCode -ne 0) {
        throw 'The WIP database migration script failed.'
    }
}
else {
    $historyExistsQuery = 'SELECT to_regclass(''public."__EFMigrationsHistory"'') IS NOT NULL;'
    $historyExists = ($historyExistsQuery | & docker exec -i erp-main-wip-postgres psql -U erp -d ErpGeneral -At).Trim()
    if ($LASTEXITCODE -ne 0 -or $historyExists -ne 't') {
        throw 'The isolated WIP database is non-empty but has no EF migration history; refusing to alter it.'
    }
}

$latestMigrationQuery = 'SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
$latestMigration = ($latestMigrationQuery | & docker exec -i erp-main-wip-postgres psql -U erp -d ErpGeneral -At).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($latestMigration)) {
    throw 'The isolated WIP database has no applied EF migrations.'
}

Write-Output 'Isolated ERP WIP infrastructure is ready.'
Write-Output 'Database: erp-main-wip-postgres / ErpGeneral / 127.0.0.1:55434'
Write-Output "Latest migration: $latestMigration"
Write-Output 'Keycloak realm: erp-main-wip at http://localhost:8183'
Write-Output 'ERP admin frontend: http://localhost:5177 (start with run-main-erp-wip.ps1 -Service admin)'
Write-Output 'Supplier Portal: http://localhost:5178 (start with run-main-erp-wip.ps1 -Service supplier-portal)'
Write-Output 'API: http://localhost:5103 (start with run-main-erp-wip.ps1 -Service api)'
Write-Output "Local credentials are stored in the ignored file: $secretFile"
