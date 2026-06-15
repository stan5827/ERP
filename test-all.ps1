$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

Push-Location $PSScriptRoot
try {
    Write-Host '=== ERP 全量測試開始 ==='

    Write-Host '1) 後端還原與測試'
    Invoke-Checked dotnet restore .\ERP.sln
    Invoke-Checked dotnet test .\ERP.sln --no-restore

    Write-Host '2) 前端安裝與測試'
    Set-Location .\erp-frontend
    Invoke-Checked npm install
    Invoke-Checked npm run test
    Invoke-Checked npx playwright install chromium
    Invoke-Checked npm run e2e

    Write-Host '=== ERP 全量測試完成 ==='
} finally {
    Pop-Location
}
