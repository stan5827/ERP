$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$secretFile = Join-Path $PSScriptRoot '.env.local'
$settings = @{}

if (!(Test-Path $secretFile)) {
    throw "Local Keycloak credentials are missing: $secretFile"
}

Get-Content $secretFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        $settings[$matches[1].Trim()] = $matches[2]
    }
}

$requiredSettings = @(
    'SUPPLIER_IDENTITY_ADMIN_CLIENT_ID',
    'SUPPLIER_IDENTITY_ADMIN_CLIENT_SECRET',
    'MINIO_ROOT_USER',
    'MINIO_ROOT_PASSWORD'
)

foreach ($key in $requiredSettings) {
    if ([string]::IsNullOrWhiteSpace($settings[$key])) {
        throw "Required local setting is missing: $key"
    }
}

if (!(Test-NetConnection -ComputerName 127.0.0.1 -Port 55432 -InformationLevel Quiet)) {
    throw 'The isolated WIP test database is not reachable on port 55432.'
}

$backendProject = Join-Path $repositoryRoot 'src\ErpBackend'
$backendAssembly = Join-Path $backendProject 'bin\Debug\net8.0\ErpBackend.dll'
if (!(Test-Path $backendAssembly)) {
    throw "Backend assembly not found. Build src\ErpBackend\ErpBackend.csproj first."
}

$env:DB_CONNECTION_STRING = 'Host=127.0.0.1;Port=55432;Database=ErpGeneral;Username=erp'
$env:JWT_SECRET_KEY = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_CONTENTROOT = $backendProject
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5102'
$env:SupplierIdentity__Authority = 'http://localhost:8182/realms/supplier-portal'
$env:SupplierIdentity__Audience = 'supplier-e2e-api'
$env:SupplierIdentity__AllowHttpMetadata = 'true'
$env:SupplierIdentity__AdminBaseUrl = 'http://localhost:8182/admin/realms/supplier-portal'
$env:SupplierIdentity__AdminClientId = $settings['SUPPLIER_IDENTITY_ADMIN_CLIENT_ID']
$env:SupplierIdentity__AdminClientSecret = $settings['SUPPLIER_IDENTITY_ADMIN_CLIENT_SECRET']
$env:CapaAttachments__MinioServiceUrl = 'http://127.0.0.1:9000'
$env:CapaAttachments__MinioBucket = 'supplier-portal-ap-attachments'
$env:CapaAttachments__MinioAccessKey = $settings['MINIO_ROOT_USER']
$env:CapaAttachments__MinioSecretKey = $settings['MINIO_ROOT_PASSWORD']
$env:CapaAttachments__MinioRegion = 'us-east-1'
$env:CapaAttachments__ClamAvHost = '127.0.0.1'
$env:CapaAttachments__ClamAvPort = '3310'
$env:CapaAttachments__ClamAvTimeoutSeconds = '60'
$env:CapaAttachments__AllowInsecureLocalMinio = 'true'

Write-Host 'Starting isolated supplier Portal API at http://localhost:5102.'
Write-Host 'This process uses only the WIP test database on port 55432.'

# Run outside the repository so DotNetEnv cannot load ERP deployment secrets.
Push-Location $env:TEMP
try {
    dotnet $backendAssembly
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
