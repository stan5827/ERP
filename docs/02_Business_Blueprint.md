# 企業 ERP 系統業務藍圖 (To-Be / Blueprint)

## 1. 系統整體架構概念與設計原則
本 ERP 系統旨在打造一個**資料單一真相來源 (Single Source of Truth)** 的企業管理平台，透過 11 大核心模組的緊密耦合，確保業務與後勤資料無縫流動。

### 1.1 全域設計原則 (Design Principles)
- **單一資料來源:** 所有交易以 ERP 系統紀錄為準，不接受 Excel 作為正式憑證或結帳依據。
- **事件驅動與狀態流轉:** 盡可能以系統狀態流轉 (Workflow) 取代人工記憶步驟 (例如單據狀態由 Draft -> Confirmed 自動觸發後續動作)。
- **可追溯性 (Traceability):** 每一張單據皆具備 Source Document Reference，能往前追溯來源單、往後追蹤後續結果 (例如：PR → PO → GR → AP → Payment)，並保留完整的審計軌跡。

## 2. 核心 11 大模組概覽
1. **儀表板 (Dashboard)**: 高階 KPI 監控。
2. **銷售管理 (Sales)**: 客戶、報價、接單。
3. **採購管理 (Procurement)**: 供應商、請購、採購、收料。
4. **庫存管理 (Inventory)**: SKU 主檔、多倉儲、安全庫存與補貨。
5. **生產製造 (MES)**: 工單、途程、報工、報廢。
6. **物料需求計畫 (MRP)**: 需求展開與推薦計畫。
7. **進階排程系統 (APS)**: 產能負荷評估與最佳化排程。
8. **研發與工程 (R&D)**: BOM 與 ECN 管理。
9. **品質保證 (QA)**: 進料 (IQC) 與成品檢驗 (OQC)。
10. **人力資源 (HR)**: 出勤與請假。
11. **財務與成本 (Finance)**: 應收/應付、實際成本結算。

## 3. 跨模組核心資料流與例外處理 (Data Flow & Exceptions)

### 3.1 報價到收款 (Quote-to-Cash)
1. **正常流程:** 報價 (Quote) → 接單 (Sales Order) → 出貨 (Inventory Issue) → 收款 (AR/Finance)。
2. **例外情境:** 
   - **銷貨退回 (Sales Return):** 觸發退貨入庫，若為瑕疵品則移入不合格倉並觸發 QA 重驗。
   - **折讓與貸項 (Credit Note):** 針對單價爭議開立 Credit Note，調整財務應收帳款但不影響庫存數量。

### 3.2 採購到付款 (Procure-to-Pay)
1. **正常流程:** 需求發生 (PR) → 採購單 (PO) → 收料檢驗 (GR/IQC) → 入庫 (Inventory) → 付款 (AP/Finance)。
2. **例外情境:**
   - **超收/短收:** 系統設定「允收百分比上限 (Delivery Tolerance)」，超出容許範圍的收料系統將自動阻擋或需走特批流程。
   - **IQC 驗退:** 進料檢驗不合格，自動產生退回供應商紀錄，並同步扣減應付帳款 (產生 Debit Note) 且不計入可用庫存。

### 3.3 計畫到生產 (Plan-to-Produce)
1. **正常流程:** 需求展開 (MRP) → 工單排程 (APS/MES) → 發料 (Inventory) → 生產報工 (WIP) → 完工入庫 (Inventory) → 成本結算 (Finance)。
2. **例外情境:**
   - **重大報廢與超投產:** 生產過程中若報廢數超過允許損耗，工單狀態暫停結案，觸發「補料申請」或「緊急補單 (Rework Order)」流程。
   - **缺料停工:** MES 回報缺料事件，緊急通知採購並由 APS 重新計算工單起訖日。
