param(
    [switch]$NoRun
)

$ErrorActionPreference = 'Stop'

function Import-DotEnvFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf('=')
        if ($separatorIndex -lt 1) {
            continue
        }

        $name = $trimmed.Substring(0, $separatorIndex).Trim()
        $value = $trimmed.Substring($separatorIndex + 1)

        if ($value.Length -ge 2) {
            $first = $value[0]
            $last = $value[$value.Length - 1]
            if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($name)) {
            [Environment]::SetEnvironmentVariable($name, $value, 'Process')
        }
    }
}

$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) {
    $root = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$backendRoot = Join-Path $root 'ErpBackend'
$seedProject = Join-Path $root 'seed-admin\seed-admin.csproj'
$envFile = Join-Path $backendRoot '.env'

if (-not (Test-Path -LiteralPath $seedProject)) {
    throw "Seeder project not found: $seedProject"
}

Import-DotEnvFile -Path $envFile

$dbConnection = [Environment]::GetEnvironmentVariable('DB_CONNECTION_STRING')
if ([string]::IsNullOrWhiteSpace($dbConnection)) {
    throw 'DB_CONNECTION_STRING was not found in ErpBackend\\.env or the current environment.'
}

$adminPassword = [Environment]::GetEnvironmentVariable('ADMIN_INITIAL_PASSWORD')
if ([string]::IsNullOrWhiteSpace($adminPassword)) {
    throw 'ADMIN_INITIAL_PASSWORD was not found in ErpBackend\\.env or the current environment.'
}

if ($NoRun) {
    Write-Host 'Seeder environment is ready. Run skipped because -NoRun was supplied.'
    Write-Host "Seeder:   $seedProject"
    Write-Host "Database: $dbConnection"
    exit 0
}

Write-Host 'Running admin seeding...'
Write-Host "Seeder:   $seedProject"

Push-Location $root
try {
    $backendProject = Join-Path $backendRoot 'ErpBackend.csproj'
    dotnet ef database update --project $backendProject --context ErpGeneralDbContext
    dotnet run --project $seedProject
} finally {
    Pop-Location
}
