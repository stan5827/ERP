param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('api', 'admin', 'supplier-portal')]
    [string]$Service
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$secretFile = Join-Path $PSScriptRoot '.local\main-erp-wip.env'
$settings = @{}

if (!(Test-Path -LiteralPath $secretFile)) {
    throw 'WIP credentials are missing. Run prepare-main-erp-wip.ps1 first.'
}

Get-Content -LiteralPath $secretFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        $settings[$matches[1].Trim()] = $matches[2]
    }
}

function Set-RequiredEnvironment([string]$Name, [string]$Key) {
    $value = $settings[$Key]
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required local setting is missing: $Key"
    }
    [Environment]::SetEnvironmentVariable($Name, $value, 'Process')
}

switch ($Service) {
    'api' {
        $projectDirectory = Join-Path $repositoryRoot 'src\ErpBackend'
        $assembly = Join-Path $projectDirectory 'bin\Release\net8.0\ErpBackend.dll'
        if (!(Test-Path -LiteralPath $assembly)) {
            throw 'Backend assembly is missing. Run prepare-main-erp-wip.ps1 first.'
        }
        if (!(Test-NetConnection -ComputerName 127.0.0.1 -Port 55434 -InformationLevel Quiet)) {
            throw 'The dedicated WIP database is not reachable on port 55434.'
        }
        if (!(Test-NetConnection -ComputerName 127.0.0.1 -Port 8183 -InformationLevel Quiet)) {
            throw 'The dedicated WIP Keycloak server is not reachable on port 8183.'
        }

        Set-RequiredEnvironment 'JWT_SECRET_KEY' 'MAIN_ERP_WIP_JWT_SECRET'
        Set-RequiredEnvironment 'ADMIN_INITIAL_PASSWORD' 'MAIN_ERP_WIP_ADMIN_INITIAL_PASSWORD'
        Set-RequiredEnvironment 'SupplierIdentity__AdminClientId' 'MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_ID'
        Set-RequiredEnvironment 'SupplierIdentity__AdminClientSecret' 'MAIN_ERP_WIP_KEYCLOAK_SERVICE_CLIENT_SECRET'
        $env:DB_CONNECTION_STRING = 'Host=127.0.0.1;Port=55434;Database=ErpGeneral;Username=erp;Password=' + $settings['MAIN_ERP_WIP_POSTGRES_PASSWORD']
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ASPNETCORE_CONTENTROOT = $projectDirectory
        $env:ASPNETCORE_URLS = 'http://127.0.0.1:5103'
        $env:SupplierIdentity__Authority = 'http://localhost:8183/realms/erp-main-wip'
        $env:SupplierIdentity__Audience = 'supplier-portal-api'
        $env:SupplierIdentity__AllowHttpMetadata = 'true'
        $env:SupplierIdentity__AdminBaseUrl = 'http://localhost:8183/admin/realms/erp-main-wip'
        $env:Cors__AdditionalOrigins__0 = 'http://localhost:5177'
        $env:Cors__AdditionalOrigins__1 = 'http://localhost:5178'

        Write-Output 'Starting the ERP API against the isolated WIP database and Keycloak.'
        Write-Output 'API listens only on 127.0.0.1:5103. Press Ctrl+C to stop.'
        Push-Location $env:TEMP
        try {
            dotnet $assembly
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }
        finally {
            Pop-Location
        }
    }
    'admin' {
        $frontendDirectory = Join-Path $repositoryRoot 'src\erp-frontend'
        if (!(Test-Path -LiteralPath (Join-Path $frontendDirectory 'node_modules'))) {
            throw 'ERP frontend dependencies are missing. Run npm ci in src\erp-frontend first.'
        }
        $env:ERP_API_PROXY_TARGET = 'http://127.0.0.1:5103'
        $env:ERP_FRONTEND_PORT = '5177'
        Write-Output 'Starting the isolated ERP admin frontend at http://localhost:5177.'
        Write-Output 'The frontend proxies API requests only to the WIP API on port 5103.'
        Write-Output 'Press Ctrl+C to stop.'
        Push-Location $frontendDirectory
        try {
            npm run dev -- --host 127.0.0.1
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }
        finally {
            Pop-Location
        }
    }
    'supplier-portal' {
        $portalDirectory = Join-Path $repositoryRoot 'src\erp-supplier-portal'
        if (!(Test-Path -LiteralPath (Join-Path $portalDirectory 'node_modules'))) {
            throw 'Supplier Portal dependencies are missing. Run npm ci in src\erp-supplier-portal first.'
        }
        $env:SUPPLIER_PORTAL_API_PROXY_TARGET = 'http://127.0.0.1:5103'
        $env:SUPPLIER_PORTAL_PORT = '5178'
        $env:VITE_KEYCLOAK_AUTHORITY = 'http://localhost:8183/realms/erp-main-wip'
        $env:VITE_KEYCLOAK_CLIENT_ID = 'erp-main-wip-supplier-portal'
        $env:VITE_KEYCLOAK_SCOPE = 'openid profile email'
        Write-Output 'Starting the isolated supplier Portal at http://localhost:5178.'
        Write-Output 'OIDC requests use only the dedicated WIP Keycloak realm on port 8183.'
        Write-Output 'Press Ctrl+C to stop.'
        Push-Location $portalDirectory
        try {
            npm run dev -- --host 127.0.0.1
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }
        finally {
            Pop-Location
        }
    }
}
