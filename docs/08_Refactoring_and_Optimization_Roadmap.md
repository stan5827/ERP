# 現有系統修正與優化計畫 (Refactoring & Optimization Roadmap)

基於最新修訂的專案章程、業務藍圖與系統架構設計，現有 ERP 系統（前端 React + 後端 .NET 8）與理想的「企業級標準」仍存在落差。為確保系統在上線前具備足夠的穩定性、可擴展性與合規性，特擬定此四階段修正優化計畫。

---

## 1. 現狀落差分析 (Gap Analysis)

| 檢核維度 | 當前系統現狀 | 目標標準 (To-Be) | 優先級 |
|---|---|---|---|
| **API 架構** | 路由為 `/api/erp/`，缺乏版本號；日誌僅依賴內建 Console。 | 升級為 `/api/v1/erp/`，導入 Serilog 集中化日誌與效能監控。 | 🔥 High |
| **資料庫 (ERD)** | 欄位缺少全域追蹤；缺乏幣別、稅務、發票等跨國/合規欄位。 | 全面補齊 `CreatedBy`, `ModifiedBy`, `Status`，以及雙軌金額 (原幣/本位幣) 與稅率。 | 🔥 High |
| **效能與排程** | MRP 展開、成本計算採同步請求，大量資料易造成前端 Timeout。 | 引入 Hangfire/Quartz 背景非同步處理；大量寫入採 Bulk Insert。 | ⭐ Medium |
| **業務防呆與例外** | 僅實作 Happy Path，缺乏退貨、折讓、超/短收容差卡控。 | 實作狀態機鎖定 (SO Confirmed 後禁改)、退貨與 Debit Note 連動。 | ⭐ Medium |

---

## 2. 階段性修正與優化路徑 (Phased Execution Plan)

### Phase 1: 系統底座升級與合規化 (Foundation Upgrade)
*目標：確保資料庫 Schema 具備完整的審計軌跡，並統一前後端通訊規範。預計耗時：1 週。*
- **[DB] 執行 EF Core Migration:** 
  - 所有 Table 補齊 Audit Trail (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`)。
  - 訂單與發票表新增 `Currency`, `ExchangeRate`, `TaxCode`, `BaseAmount` 等財務欄位。
- **[Backend] API 版本化與監控:** 
  - 重構 Controller Route 加上 `v1`。
  - 整合 Serilog 並配置 Global Exception Handling (統一攔截並轉換為 `ApiResponse`)。
- **[Frontend] API Client 更新:** 
  - 調整 `api.ts` 的 Base URL，並實作攔截器 (Interceptor) 處理 Token 過期與 `5xx` 系統異常提示。

### Phase 2: 核心防呆與不可變性落實 (Core Guardrails & Immutability)
*目標：防止髒資料產生，確保業務單據的狀態機嚴格執行。預計耗時：2 週。*
- **[Backend] 狀態機卡控 (State Machine Locks):** 
  - 針對 Sales Orders 與 Purchase Orders，只要狀態到達 `Confirmed`，後端 API 拒絕任何變更單價或數量的 `PUT` 請求（必須走 CR 流程）。
- **[Backend & DB] 庫存不可變性 (Inventory Immutability):** 
  - 拔除 `InventoryTransactions` 的 `Update` 與 `Delete` API。
  - 實作「反向沖銷單 (Reversal)」功能以因應人員操作失誤。
- **[Frontend] UI 防呆反饋:** 
  - 訂單鎖定後，前端對應的 Input 欄位自動切換為 `disabled`。
  - 實作防連點 (Debounce) 與送出時的 Loading Overlay。

### Phase 3: 例外情境與跨模組連動 (Exception Flows & Integration)
*目標：補齊退貨、折讓與品保驗退的複雜情境。預計耗時：2 週。*
- **[Procurement & QA] 允收容差與驗退:** 
  - 收料 (GR) API 實作容差百分比驗證。
  - IQC 驗退自動觸發「退回供應商 (Return to Vendor)」，並透過事件總線 (Event Bus / Service) 通知 Finance 模組產生 Debit Note。
- **[Sales] 銷貨退回:** 
  - 建立 Sales Return API，聯動庫存入庫 (暫存區或不良品區) 以及 AR 沖退。

### Phase 4: 效能調優與非同步化 (Performance & Async Operations)
*目標：解決巨量資料運算造成的阻塞，確保系統高可用。預計耗時：1.5 週。*
- **[Backend] 導入 Hangfire:** 
  - 將 MRP 展開演算法移至 Background Job 執行，前端改為 Polling 查詢狀態或接收 SignalR 推播。
  - 月底 Actual Costing 結算改為背景排程。
- **[Backend] DB 寫入優化:** 
  - 針對期初開帳、巨量 BOM 展開寫入，捨棄單筆 `SaveChanges()`，改用 `EFCore.BulkExtensions` 或直接呼叫 SQL Bulk Copy。
- **[Frontend] 狀態進度條:** 
  - 針對背景任務提供進度監控介面 (Task Monitor UI)。

---

## 3. 下一步行動建議 (Next Steps)

1. **技術驗證 (POC):** 優先由架構師針對 Phase 4 的 Hangfire 與 Phase 1 的 EF Core 審計欄位攔截器 (SaveChangeInterceptor) 進行雛型驗證。
2. **開立工單 (Jira Tasks):** 將上述 Phase 1 到 Phase 4 轉化為具體的 Jira Epics 與 Stories，並分派給前後端開發者。
3. **Schema Review 會議:** 針對新增的財務與追蹤欄位，與相關 Key Users 開會確認欄位定義無誤後再進行 Migration 部署。
