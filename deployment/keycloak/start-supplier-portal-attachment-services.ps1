$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$secretFile = Join-Path $PSScriptRoot '.env.local'
$composeFile = Join-Path $PSScriptRoot 'docker-compose.attachments.local.yml'
$settings = @{}

if (!(Test-Path -LiteralPath $secretFile)) {
    throw "Local credentials file is missing: $secretFile"
}

$ignoredPath = git -C $repositoryRoot check-ignore $secretFile
if ($LASTEXITCODE -ne 0) {
    throw 'The local credentials file is not Git-ignored; refusing to store attachment-service credentials.'
}

Get-Content -LiteralPath $secretFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        $settings[$matches[1].Trim()] = $matches[2]
    }
}

$append = ''
if ([string]::IsNullOrWhiteSpace($settings['MINIO_ROOT_USER'])) {
    $settings['MINIO_ROOT_USER'] = 'portaltest'
    $append += "MINIO_ROOT_USER=$($settings['MINIO_ROOT_USER'])`r`n"
}
if ([string]::IsNullOrWhiteSpace($settings['MINIO_ROOT_PASSWORD'])) {
    $settings['MINIO_ROOT_PASSWORD'] = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
    $append += "MINIO_ROOT_PASSWORD=$($settings['MINIO_ROOT_PASSWORD'])`r`n"
}

if ($append) {
    [System.IO.File]::AppendAllText($secretFile, "`r`n$append")
}

foreach ($key in @('MINIO_ROOT_USER', 'MINIO_ROOT_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace($settings[$key])) {
        throw "Required local setting is missing: $key"
    }
}

$minioPort = Get-NetTCPConnection -LocalPort 9000 -State Listen -ErrorAction SilentlyContinue
if ($minioPort -and $minioPort.LocalAddress -notcontains '127.0.0.1') {
    throw 'Port 9000 is already bound beyond loopback; refusing to start local attachment services.'
}
$clamPort = Get-NetTCPConnection -LocalPort 3310 -State Listen -ErrorAction SilentlyContinue
if ($clamPort -and $clamPort.LocalAddress -notcontains '127.0.0.1') {
    throw 'Port 3310 is already bound beyond loopback; refusing to start local attachment services.'
}

docker compose --env-file $secretFile -p erp-supplier-portal-attachments -f $composeFile up -d
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to start isolated local attachment services.'
}

$deadline = (Get-Date).AddMinutes(5)
$minioReady = $false
$clamavReady = $false
while ((Get-Date) -lt $deadline) {
    if (!$minioReady) {
        try {
            $response = Invoke-WebRequest -Uri 'http://127.0.0.1:9000/minio/health/live' -UseBasicParsing -TimeoutSec 2
            $minioReady = $response.StatusCode -eq 200
        }
        catch {
            $minioReady = $false
        }
    }
    if (!$clamavReady) {
        $clamavReady = Test-NetConnection -ComputerName 127.0.0.1 -Port 3310 -InformationLevel Quiet -WarningAction SilentlyContinue
    }
    if ($minioReady -and $clamavReady) {
        Write-Output 'Local AP attachment storage and malware scanner are ready on loopback.'
        exit 0
    }
    Start-Sleep -Seconds 3
}

docker compose --env-file $secretFile -p erp-supplier-portal-attachments -f $composeFile ps
throw "Attachment services did not become ready before timeout (MinIO ready: $minioReady; ClamAV ready: $clamavReady)."
