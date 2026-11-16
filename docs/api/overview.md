# API 設計規範與相容性管理

> 來源：`src/docs/05_API_Specifications.md`

本系統採用 RESTful 架構進行前後端分離設計。所有 API 請求採用標準 JSON 格式進行資料交換。

## 1. API 設計規範

### 1.1 版本控管與相容性 (Versioning)

- 所有 API Base Path 強制包含版本號：`/api/v1/erp/...`。
- 若未來發生 Breaking Changes（如欄位型別變更、刪除必填欄位），必須另開 `/api/v2/...`。
- 舊版 API 進入 Deprecation Policy：原則上保留舊版至少 6 個月，並於 Header 回傳 `Warning: 299 Deprecated API`。

### 1.2 統一回應格式 (ApiResponse)

繼承 `BaseController`，回應格式強制包裝為：

```json
{
  "success": true,
  "message": "Success",
  "data": { ... }
}
```

### 1.3 日誌與監控 (Logging & Monitoring)

- **集中式日誌**：後端整合 Serilog，將所有 Request/Response 狀態、Error Log 拋轉至集中式日誌系統（如 Elasticsearch/Kibana）。
- **效能監控指標**：於架構層次收集關鍵 API 的 QPS（每秒請求數）、平均 Latency（延遲時間）與 HTTP 5xx 失敗率，當異常飆高時觸發告警。

## 2. 認證與授權

- `POST /api/v1/auth/login`：回傳 JWT Token。
- API 藉由 `[Authorize]` 與 RBAC 原則實行存取控制。

### 2.1 安全設定

- JWT 密鑰透過環境變數 `JWT_SECRET_KEY` 設定，長度至少 32 字元。
- 預設管理員帳號 `admin`，首次登入強制修改密碼。
- CORS 白名單：`http://localhost:5173`、`5174`、`5175`。

## 3. 健康檢查與監控端點

| 端點 | 說明 |
|------|------|
| `GET /` | 服務狀態確認 |
| `GET /health` | 健康檢查（含資料庫連線） |
| `GET /health/ready` | Ready 檢查（詳細檢查項目） |
| `GET /metrics` | Prometheus 指標（prometheus-net） |

## 4. 錯誤處理

- 全域 Exception Handler 統一處理例外。
- 常見錯誤碼：
  - `400`：參數驗證失敗
  - `401`：未認證
  - `403`：無權限
  - `404`：資源不存在
  - `500`：伺服器內部錯誤

