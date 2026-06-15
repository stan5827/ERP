param(
    [switch]$NoLaunch,
    [switch]$SkipDbCheck
)

$ErrorActionPreference = 'Stop'

function Resolve-ConnectionSetting {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,
        [Parameter(Mandatory = $true)]
        [string[]]$Keys,
        [string]$Default = ''
    )

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        return $Default
    }

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.ConnectionString = $ConnectionString

    foreach ($key in $Keys) {
        foreach ($existingKey in $builder.Keys) {
            if ($existingKey -is [string] -and $existingKey.Equals($key, [System.StringComparison]::OrdinalIgnoreCase)) {
                return [string]$builder[$existingKey]
            }
        }
    }

    return $Default
}

function Ensure-EnvironmentValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Prompt
    )

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = Read-Host $Prompt
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Name is required."
    }

    [Environment]::SetEnvironmentVariable($Name, $value, 'Process')
    return $value
}

function Escape-PowerShellSingleQuotedString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.Replace("'", "''")
}

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

function Get-PostgresServices {
    Get-Service | Where-Object {
        $_.Name -match 'postgres' -or $_.DisplayName -match 'postgres'
    }
}

function Ensure-PostgresRunning {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ComputerName,
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $services = @(Get-PostgresServices)
    if ($services.Count -eq 0) {
        throw "No PostgreSQL Windows service was found. Install PostgreSQL or start it manually, then run this script again."
    }

    foreach ($service in $services) {
        if ($service.Status -ne 'Running') {
            Write-Host "Starting PostgreSQL service: $($service.Name)"
            Start-Service -Name $service.Name
        }
    }

    $deadline = (Get-Date).AddSeconds(60)
    do {
        if (Test-NetConnection -ComputerName $ComputerName -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) {
            return
        }

        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "PostgreSQL service started, but $ComputerName`:$Port is still not reachable after 60 seconds."
}

$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) {
    $root = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$backendRoot = Join-Path $root 'ErpBackend'
$frontendRoot = Join-Path $root 'erp-frontend'
$backendProject = Join-Path $backendRoot 'ErpBackend.csproj'

if (-not (Test-Path -LiteralPath $backendProject)) {
    throw "Backend project not found: $backendProject"
}

if (-not (Test-Path -LiteralPath $frontendRoot)) {
    throw "Frontend folder not found: $frontendRoot"
}

Import-DotEnvFile -Path (Join-Path $backendRoot '.env')

$connectionString = [Environment]::GetEnvironmentVariable('DB_CONNECTION_STRING')
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw 'DB_CONNECTION_STRING was not found in ErpBackend\\.env or the current environment.'
}

$jwtSecret = [Environment]::GetEnvironmentVariable('JWT_SECRET_KEY')
if ([string]::IsNullOrWhiteSpace($jwtSecret)) {
    throw 'JWT_SECRET_KEY was not found in ErpBackend\\.env or the current environment.'
}

if ($jwtSecret.Length -lt 32) {
    throw 'JWT_SECRET_KEY must be at least 32 characters.'
}

$dbHost = Resolve-ConnectionSetting -ConnectionString $connectionString -Keys @('Host', 'Server') -Default 'localhost'
$portText = Resolve-ConnectionSetting -ConnectionString $connectionString -Keys @('Port') -Default '5432'
$port = 5432
[void][int]::TryParse($portText, [ref]$port)

if (-not $SkipDbCheck) {
    Ensure-PostgresRunning -ComputerName $dbHost -Port $port

    $dbReachable = $false
    try {
        $dbReachable = Test-NetConnection -ComputerName $dbHost -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
    } catch {
        $dbReachable = $false
    }

    if (-not $dbReachable) {
        throw "PostgreSQL is not reachable at $dbHost`:$port. Start the database first, then run this script again."
    }
}

if ($NoLaunch) {
    Write-Host 'Environment is ready. Launch was skipped because -NoLaunch was supplied.'
    Write-Host "Backend:  $backendProject"
    Write-Host "Frontend: $frontendRoot"
    exit 0
}

$psExe = (Get-Command powershell.exe).Source
$dbConnectionLiteral = Escape-PowerShellSingleQuotedString $connectionString
$jwtSecretLiteral = Escape-PowerShellSingleQuotedString $jwtSecret

Write-Host 'Starting backend and frontend in separate windows...'
Write-Host 'Backend:  http://localhost:5100/swagger'
Write-Host 'Frontend: http://localhost:5173'

Start-Process -FilePath $psExe -WorkingDirectory $backendRoot -ArgumentList @(
    '-NoLogo',
    '-NoExit',
    '-ExecutionPolicy',
    'Bypass',
    '-Command',
    "`$env:DB_CONNECTION_STRING='$dbConnectionLiteral'; `$env:JWT_SECRET_KEY='$jwtSecretLiteral'; dotnet run --project `"$backendProject`" --launch-profile http"
)

Start-Process -FilePath $psExe -WorkingDirectory $frontendRoot -ArgumentList @(
    '-NoLogo',
    '-NoExit',
    '-ExecutionPolicy',
    'Bypass',
    '-Command',
    "if (-not (Test-Path -LiteralPath 'node_modules')) { npm install }; npm run dev"
)
