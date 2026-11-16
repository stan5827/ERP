# 系統架構與資料庫設計 (Architecture & ERD)

> 來源：`src/docs/04_System_Architecture_and_ERD.md`

## 1. 系統架構與效能設計 (System Architecture)

- **前端**：React 18, TypeScript, Vite, shadcn/ui。
- **後端**：ASP.NET Core 8.0 Web API, EF Core。
- **跨模組設計原則**：
  - **效能與併發控制**：針對耗時任務（如 MRP 展開、期初庫存大量匯入、月結成本計算），後端採用背景排程伺服器（如 Hangfire 或 Quartz）進行非同步處理，避免阻塞線上交易 API。
  - **巨量寫入**：針對日誌、異動歷史等大量寫入，使用 Bulk Insert / `SqlBulkCopy` 機制，並嚴格控制資料庫 Transaction 的 Scope 範圍，降低長時間鎖表（Table Lock）的風險。

## 2. 核心資料庫設計 (ERD & Data Model)

本系統資料庫高度正規化以確保資料一致性。

### 2.1 核心資料表關聯概述

1. **基礎主檔**：`Users`, `Roles`, `Skus`, `Customers`, `Suppliers`。
2. **供應鏈與庫存**：`InventoryTransactions`, `SalesOrders`, `PurchaseOrders`, `GoodsReceipts`。
3. **生產製造**：`Boms`, `WorkCenters`, `Routings`, `WorkOrders`, `WipReports`。
4. **工程變更**：`Ecrs`, `Ecns`。
5. **財務結算**：`ActualCostings`（關聯工單，儲存材料/人工/費用明細與差異分析）。

### 2.2 模型檔案（後端）

後端模型位於 `src/ErpBackend/Models/`，依模組拆分：

| 檔案 | 類數 | 行數 | 說明 |
|------|-----:|-----:|------|
| InventoryModels.cs | 24 | 489 | 庫存主檔、交易、庫別 |
| HrModels.cs | 19 | 443 | 人事、組織、考勤 |
| ProcurementModels.cs | 19 | 390 | 採購、供應商 |
| SalesModels.cs | 15 | 357 | 銷售、報價 |
| MesModels.cs | 16 | 274 | 製造執行、工單 |
| CollaborationModels.cs | - | - | 協作、行事曆 |
| FinanceModels.cs | - | - | 財務、成本 |
| AIModels.cs | - | - | AI Agent 相關 |

> 完整清單請參考 `src/ErpBackend/Models/` 目錄。

