@echo off
cd /d %~dp0
echo === ERP 全量測試開始 ===

echo 1) 後端還原與測試
call dotnet restore ERP.sln
call dotnet test ERP.sln --no-restore

echo 2) 前端安裝與測試
cd erp-frontend
call npm install
call npm run test

echo === ERP 全量測試完成 ===
