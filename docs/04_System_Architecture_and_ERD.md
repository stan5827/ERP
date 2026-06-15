# 系統架構與資料庫設計 (Architecture & ERD)

## 1. 系統架構與效能設計 (System Architecture)
- **前端:** React 18, TypeScript, Vite, shadcn/ui。
- **後端:** ASP.NET Core 8.0 Web API, EF Core。
- **跨模組設計原則:**
  - **效能與併發控制:** 針對耗時任務 (如 MRP 展開、期初庫存大量匯入、月結成本計算)，後端採用背景排程伺服器 (如 Hangfire 或 Quartz) 進行非同步處理，避免阻塞線上交易 API。
  - **巨量寫入:** 針對日誌、異動歷史等大量寫入，使用 Bulk Insert / `SqlBulkCopy` 機制，並嚴格控制資料庫 Transaction 的 Scope 範圍，降低長時間鎖表 (Table Lock) 的風險。

## 2. 核心資料庫設計 (ERD & Data Model)

本系統資料庫高度正規化以確保資料一致性，並納入以下關鍵設計：

### 2.1 全域欄位與審計軌跡 (Audit Trail)
所有交易與主檔表皆必須實作以下欄位以確保留痕：
- `CreatedBy`, `CreatedAt`
- `ModifiedBy`, `ModifiedAt`
- `Status` (支援邏輯刪除或狀態流轉)
針對關鍵交易 (如 `InventoryTransactions`, `ActualCostings`) 遵循**不可變更原則 (Immutability)**，一經寫入不得 Update，僅能透過沖銷單據更正。

### 2.2 多幣別與稅務設計
為確保跨國交易與財務一致性，引入以下表與欄位：
- **Currencies & ExchangeRates:** 記錄每日/每月匯率來源與生效日。
- **單據金額雙軌制:** 銷售與採購單據的明細中，必須同時記錄「原幣金額 (Document Currency)」與「本位幣金額 (Base Currency)」，避免後續結帳因歷史匯率浮動導致財務報表不平。
- **稅務與發票:** 財務與訂單表內建 `TaxCode`, `TaxRate`, `IsTaxInclusive` 欄位，並預留 `InvoiceNumber` 欄位以利未來串接台灣電子發票 (E-Invoice) 或 EDI 閘道。

### 2.3 核心資料表關聯概述
1. **基礎主檔:** `Users`, `Roles`, `Skus`, `Customers`, `Suppliers`。
2. **供應鏈與庫存:** `InventoryTransactions`, `SalesOrders`, `PurchaseOrders`, `GoodsReceipts`。
3. **生產製造:** `Boms`, `WorkCenters`, `Routings`, `WorkOrders`, `WipReports`。
4. **工程變更:** `Ecrs`, `Ecns`。
5. **財務結算:** `ActualCostings` (關聯工單，儲存材料/人工/費用明細與差異分析)。
