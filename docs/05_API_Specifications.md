# 介面與 API 規格 (API Specifications)

本系統採用 RESTful 架構進行前後端分離設計。所有 API 請求採用標準 JSON 格式進行資料交換。

## 1. API 設計規範與相容性管理

### 1.1 版本控管與相容性 (Versioning)
- 所有 API Base Path 強制包含版本號：`/api/v1/erp/...`。
- 若未來發生 Breaking Changes (如欄位型別變更、刪除必填欄位)，必須另開 `/api/v2/...`，舊版 API 進入 Deprecation Policy（原則上保留舊版至少 6 個月，並於 Header 回傳 `Warning: 299 Deprecated API`）。

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
- **集中式日誌:** 後端整合 Serilog，將所有 Request/Response 狀態、Error Log 拋轉至集中式日誌系統 (如 Elasticsearch/Kibana)。
- **效能監控指標:** 於架構層次收集關鍵 API 的 QPS (每秒請求數)、平均 Latency (延遲時間) 與 HTTP 5xx 失敗率，當異常飆高時觸發告警。

## 2. 核心模組 API 端點 (Endpoints)
*(列舉部分關鍵 Endpoint)*
- **Dashboard:** `GET /api/v1/erp/dashboard/sidebar-badges`
- **Inventory:** `GET /api/v1/erp/inventory/transactions` (查詢庫存異動軌跡)
- **Sales/Procurement:** `POST /api/v1/erp/procurement/orders/{id}/receive` (收料並驗證允收容差)
- **MES/APS:** 
  - `POST /api/v1/erp/manufacturing/wip` (提報在製品進度)
  - `POST /api/v1/erp/aps-integration/import` (接收 APS 引擎排程結果)
- **R&D:** `POST /api/v1/erp/ecns` (建立工程變更)
- **Finance:** `POST /api/v1/erp/workorders/{id}/costing/settle` (實際成本結算)

## 3. 認證與授權
- `POST /api/v1/auth/login`: 回傳 JWT Token。
- API 藉由 `[Authorize]` 與 RBAC 原則實行存取控制。
