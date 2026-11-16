$ErrorActionPreference = 'Stop'

$containerName = 'erp-wip-candidate-test-db-20261025'
$databaseName = 'ErpGeneral'
$seedFile = Join-Path $PSScriptRoot 'seed-supplier-portal-ap-test-data.sql'

$containerState = docker inspect --format '{{.State.Running}}' $containerName
if ($LASTEXITCODE -ne 0 -or $containerState -ne 'true') {
    throw "WIP database container '$containerName' is not running."
}

$publishedPort = docker port $containerName '5432/tcp'
if ($LASTEXITCODE -ne 0 -or $publishedPort -notmatch '127\.0\.0\.1:55432$') {
    throw "WIP database container '$containerName' is not bound to the expected loopback port 55432."
}

$actualDatabase = docker exec $containerName psql -U erp -d $databaseName -Atc 'SELECT current_database();'
if ($LASTEXITCODE -ne 0 -or $actualDatabase -ne $databaseName) {
    throw "Could not verify the expected WIP database '$databaseName'."
}

Get-Content -Raw -LiteralPath $seedFile |
    docker exec -i $containerName psql -v ON_ERROR_STOP=1 -U erp -d $databaseName
if ($LASTEXITCODE -ne 0) {
    throw 'The supplier-portal AP seed transaction failed.'
}

Write-Output 'Seeded synthetic supplier-portal AP source data in the isolated WIP database.'
