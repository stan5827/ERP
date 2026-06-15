# 測試計畫與關鍵流程測試案例 (Test Plan & Cases)

## 1. 測試計畫與驗收標準 (Test Strategy & Acceptance Criteria)
系統測試涵蓋單元測試、整合測試、系統整合測試 (SIT) 與使用者驗收測試 (UAT)。

### 1.1 UAT 驗收標準 (UAT Sign-off Criteria)
- **關鍵流程覆蓋:** 每個核心端到端流程 (QTC, PTP, P2P) 至少完成 5 筆以上的真實業務資料盲測。
- **缺陷修復:** 所有的 P1 (Critical) 與 P2 (High) 等級 Bug 必須被徹底關閉，或擁有業務單位認可的暫時替代方案 (Workaround)。
- **延遲處理:** P3 (Medium) 及以下的非阻斷性問題，允許列入 Go-Live 後的系統優化 Backlog。

### 1.2 Hypercare 服務等級協議 (Hypercare SLA)
系統上線後 2 週內為 Hypercare 時期，並遵循以下處理時效：
- **單一通報窗口:** 設立專屬的 Service Desk 或專案 LINE/Teams 群組，避免問題零散回報。
- **P1 問題 (系統當機、無法出貨/開發票):** 2 小時內提供 Workaround，24 小時內部署修補程式。
- **P2 問題 (核心功能異常但有替代路徑):** 24 小時內提供 Workaround，3 個工作天內修復。

## 2. 關鍵流程端到端測試案例 (E2E Test Cases)

### 2.1 Quote-to-Cash (報價到收款)
- **情境:** 客戶要求報價，隨後下單，完成出貨並收款。
- **驗證點:** 確認銷售單 Confirmed 後庫存可用量凍結；出貨後庫存扣減精確；發票/應收帳款與單據總額吻合 (含多幣別匯率轉換正確)。

### 2.2 Procure-to-Pay (採購到付款)
- **情境:** MRP 產生的 PR 轉為 PO，收料驗收後付款。
- **驗證點:** 驗證 Smart ROP 的精準度；測試 PO 超收/短收容差卡控機制；驗收不合格 (IQC 失敗) 觸發退貨與 Debit Note 生成邏輯。

### 2.3 Plan-to-Produce (計畫到生產)
- **情境:** 銷售訂單觸發 MRP，展開工單並結算成本。
- **驗證點:** 驗證 MRP 引擎展開 BOM 時的 MOQ/Lot Size 考量是否正確；WIP 報工防呆 (數量不可超報)；月底 Actual Costing 結算時材料、人工、製造費用是否準確反映於庫存成本。
