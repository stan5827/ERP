# 功能需求規格 (Functional Requirement Document - FRD)

本文件詳細定義 ERP 系統各核心模組的功能規格與關鍵業務規則。

## 1. 銷售模組 (Sales)
- **核心功能:** 客戶管理、報價單 (Quote)、銷售訂單 (Sales Order)。
- **業務規則 (Business Rules):** 
  - 銷售訂單狀態變更為 `Confirmed` 後，嚴格禁止隨意修改單價與數量；若需修改必須提出訂單變更申請 (Order Change Request) 並經主管核准。
  - 報價單支援多幣別，並應記錄當下匯率。

## 2. 採購模組 (Procurement)
- **核心功能:** 供應商評鑑、請購單 (PR)、採購單 (PO)、收料單 (GR)。
- **業務規則:** 
  - 系統支援設定供應商的最少訂購量 (MOQ) 與交貨前置時間 (Lead Time)。
  - PO 收料時，系統將依據容差上限卡控，超過允收範圍拒絕執行 GR。

## 3. 庫存模組 (Inventory)
- **核心功能:** SKU 主檔、庫存異動 (Transactions)、智慧補貨 (ROP)。
- **業務規則:** 
  - 庫存異動必須不可逆 (Immutable)。若發生操作錯誤，不允許修改歷史 Transaction，必須執行一筆「反向異動 (Reversal Transaction)」以平衡數量。

## 4. 生產製造模組 (MES)
- **核心功能:** 工單 (Work Order)、途程 (Routing)、報工 (WIP Report)、報廢紀錄。
- **業務規則:** 
  - 報工數量加總不可大於前一工序的良品數。
  - 完工入庫數量不能大於工單的實際產出良品數。

## 5. 研發模組 (R&D)
- **核心功能:** BOM 管理、ECR/ECN 工程變更。
- **業務規則:** 
  - ECN 核准並到達「生效日期 (Effective Date)」後，系統自動將舊版 BOM 設為停用 (Obsolete)，MRP 與 MES 建立新單時強制帶入最新版 BOM。

## 6. 計畫與排程模組 (MRP & APS)
- **核心功能:** 主生產排程 (MPS)、MRP 引擎、APS 整合介面。
- **業務規則:** 
  - MRP 展開計算時，必須考慮現有庫存、在途採購量、已發出的計畫工單，並合併 MOQ 與 Lot Size 計算淨需求。
  - 提供多來源供應商設定，並依配額比例 (Quota) 分配採購建議。

## 7. 財務模組 (Finance)
- **核心功能:** 實際成本結算 (Actual Costing)、應收/應付帳款 (AR/AP)。
- **業務規則:** 
  - 實際成本結算必須抓取該工單所有關聯的發料紀錄 (移動平均/標準成本)、實際報工工時 (× 費率) 與分攤製造費用。

## 8. 品質保證模組 (QA)
- **核心功能:** IQC 進料檢驗、OQC 出貨檢驗。
- **業務規則:**
  - 不良品處理支援：報廢 (Scrap)、重工 (Rework)、退回供應商 (Return to Vendor)。
  - 退回供應商時，系統必須自動拋轉資料至財務模組生成 Debit Note 以扣減應付款。

## 9. 人資模組 (HR) 與 儀表板 (Dashboard)
- **HR 功能:** 出勤與請假追蹤、薪資對接基礎欄位。
- **系統設定:** 基於 Role-Based Access Control (RBAC) 進行權限卡控。
