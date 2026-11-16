# 資料表設計規範 (Database Conventions)

> 來源：`src/docs/04_System_Architecture_and_ERD.md`

## 1. 全域欄位與審計軌跡 (Audit Trail)

所有交易與主檔表皆必須實作以下欄位以確保留痕：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `CreatedBy` | string | 建立者 |
| `CreatedAt` | DateTime | 建立時間 |
| `ModifiedBy` | string | 修改者 |
| `ModifiedAt` | DateTime | 修改時間 |
| `Status` | string | 支援邏輯刪除或狀態流轉 |

> 針對關鍵交易（如 `InventoryTransactions`, `ActualCostings`）遵循**不可變更原則 (Immutability)**，一經寫入不得 Update，僅能透過沖銷單據更正。

## 2. 多幣別與稅務設計

為確保跨國交易與財務一致性，引入以下表與欄位：

- **Currencies & ExchangeRates**：記錄每日/每月匯率來源與生效日。
- **單據金額雙軌制**：銷售與採購單據的明細中，必須同時記錄「原幣金額 (Document Currency)」與「本位幣金額 (Base Currency)」，避免後續結帳因歷史匯率浮動導致財務報表不平。
- **稅務與發票**：財務與訂單表內建 `TaxCode`, `TaxRate`, `IsTaxInclusive` 欄位，並預留 `InvoiceNumber` 欄位以利未來串接台灣電子發票 (E-Invoice) 或 EDI 閘道。

## 3. 模型設計原則

- 模型依模組拆分檔案，避免單一檔案過大。
- 導航屬性（Navigation Properties）數量應受控，避免過度關聯造成 N+1 查詢。
- 建議導入 Domain Events 記錄領域事件。

