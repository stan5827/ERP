# 上線切換計畫與資料遷移計畫 (Go-Live & Migration Plan)

## 1. 上線切換計畫 (Go-Live Strategy)

本專案將採取 **大爆炸切換 (Big Bang Adoption)** 配合 **週末停機 (Weekend Cut-over)**。為確保成功，切換前將進行嚴格的演練與效能評估。

### 1.1 Cut-over 週末腳本與排程
切換週末時間線嚴格控管，並以單一標準時區執行：

| 時間點 | 活動 | 負責角色 | 輸入 / 輸出檢核 |
|---|---|---|---|
| **週五 18:00** | 舊系統全面切換為唯讀模式 | IT + 業務部門主管 | 雙方簽核「切換確認紀錄」 |
| **週五 19:00 - 23:00** | 期末資料 Extract | IT DBA | 產出舊系統 CSV/Dump 檔案清單 |
| **週六 08:00 - 18:00** | 執行資料匯入 (ETL Load) 與初步驗證 | IT + 各模組 Data Owner | 產出匯入 Log、差異與異常報表 |
| **週日 08:00 - 12:00** | 系統健康度與資料抽查 (Sanity Check) | Key Users | 完成驗證並簽署「驗證結果簽核單」 |
| **週日 14:00** | 專案委員會舉行 Go / No-Go 會議 | 專案指導委員會 | 決議上線或啟動退回機制 |
| **週一 08:00** | ERP 系統正式上線 (Go-Live) | 全體人員 | 開放系統登入 |

### 1.2 效能要求與 Rollback 策略
- **效能/容量測試要求:** 在正式 Cut-over 前，資料遷移團隊必須證明其 ETL 腳本與程序可以在 **12 小時以內**完成全量資料遷移。若無法達成，必須優化腳本 (如採用 Bulk Insert) 或重新設計批次。
- **Point of No Return (無法回退點):** 訂於「週六 18:00」。若在此時間點前遭遇無法克服的資料損壞或匯入失敗，將啟動 **Rollback 機制**。
- **Rollback 步驟:** 取消新系統存取權、解除舊系統唯讀模式限制、IT 發出暫緩上線全公司公告。

---

## 2. 資料遷移計畫 (Data Migration Plan)

資料遷移是 ERP 失敗的高風險區。必須遵循多次演練與明確責任分工的原則。

### 2.1 多輪演練與資料所有權 (Rehearsals & Data Ownership)
- **多輪演練:** 正式切換前，必須執行 **2 到 3 次全量 Migration 演練 (Rehearsal)**。每次演練需涵蓋 Extract、Transform、Load 與 Validation，並產出 Issue List 供修正腳本。
- **資料所有權 (Data Ownership):** 
  - **IT 部門:** 僅負責技術性 ETL 腳本開發、編碼轉換與系統搬運。
  - **業務單位 (Business Owners):** 絕對負責資料內容的品質與正確性 (如：判斷哪些呆滯料號不遷移、客戶名單清洗)。正式 Cut-over 前，主管必須簽署 **Data Sign-off 表單**。

### 2.2 清洗與轉換規則 (Data Quality Rules)
在 Transform 階段套用自動化檢核：
- 不允許重複的 SKU Code 或 Customer Code。
- 必須補齊必填欄位 (如稅別、預設幣別、Item Type)，否則該筆資料丟入 Exception Report。
- 業務單位依據 Exception Report 決定手動修補或捨棄該資料。

### 2.3 驗證指標 (Validation Metrics)
資料載入後的驗證標準不僅止於「筆數比對」，必須執行深度對帳：
1. **庫存總值明細表:** 新舊系統庫存總額差異必須為 $0。
2. **財務帳表對接:** 試算表 (Trial Balance)、AR/AP 帳齡分析表 (Aging Report) 逐科目與客戶核對。
3. **營運指標:** 未結的 PO/SO 數量對比、未結工單數量與在製成本 (WIP) 的正確性核對。
