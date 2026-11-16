# Migration 回復與容器映像回退演練

## 範圍與前置條件

- 必須在 Staging 或可隔離的正式資料庫複本執行，不得直接對正式資料庫練習。
- 演練前備份資料庫；記錄目前 Migration、映像標籤及健康狀態。
- 預先取得上一個已驗證的後端與前端映像標籤。

## Migration 回復

1. 記錄目前 Migration：
   `dotnet ef migrations list --project src/ErpBackend/ErpBackend.csproj`
2. 建立資料庫備份。
3. 回復到演練前 Migration：
   `dotnet ef database update <PreviousMigration> --project src/ErpBackend/ErpBackend.csproj`
4. 啟動後端並確認 `/health` 回傳 200。
5. 驗證回復前後資料筆數與應用核心查詢一致。
6. 從資料庫備份還原至演練前狀態，並重跑 smoke test。

任何 Migration 無法正確 rollback 時，必須停止演練並修正 Migration；不得以手動刪除正式資料庫物件替代可重現程序。

## 容器映像回退

1. 記錄目前 SHA 與 `docker compose ps`。
2. 將 backend/frontend 映像切換為最後已驗證標籤。
3. `docker compose up -d backend frontend`。
4. 輪詢 `/health` 與 `http://localhost:5173/`。
5. 執行登入、設備列表及 ESS 讀取 smoke test。
6. 確認資料庫 Migration 與舊映像相容；若不相容，使用資料庫備份及對應版本組合回復。

## 完成證據

- 演練時間、操作者、來源/目標 Migration。
- 備份檔雜湊或備份識別碼。
- 切換前後映像標籤與容器 ID。
- `/health`、前端及 API 驗證結果。
- 資料筆數與核心資料一致性結果。
