# ProMES ERP 前端操作手冊

## 一、系統架構總覽

### 1.1 技術架構

| 技術項目 | 說明 |
|----------|------|
| 框架 | React + TypeScript |
| 資料獲取 | TanStack Query (React Query) |
| HTTP 客戶端 | Axios |
| 圖表 | Recharts |
| 圖標 | Lucide React |
| 構建工具 | Vite |
| API 基礎 URL | `http://localhost:5100/api` |

### 1.2 登入資訊

| 項目 | 預設值 |
|------|--------|
| 帳號 | admin |
| 密碼 | admin123 |

---

## 二、路由模組對應表

| 模組 ID | 路由標籤 | 頁面檔案 |
|---------|----------|----------|
| dashboard | 系統儀表板 | `Dashboard.tsx` |
| sales | 銷貨及收款 | `Sales.tsx` |
| procurement | 採購及付款 | `Procurement.tsx` |
| inventory | 庫存管理 | `Inventory.tsx` |
| manufacturing | 生產製造 · MES | `Manufacturing.tsx` |
| hr | 人力資源 | `HR.tsx` |
| finance | 財務管理 | `Finance.tsx` |
| assets | 固定資產 | `Assets.tsx` |
| budget | 預算規劃 | `Budget.tsx` |
| quality | 品質管理 | `Quality.tsx` |
| rd | 研發管理 | `RD.tsx` |
| esg | ESG 永續管理 | `ESG.tsx` |
| ai | AI Agent 監控 | `AI.tsx` |
| agentflows | Agent Flow | `AgentFlows.tsx` |
| collaboration | 協作平台 | `Collaboration.tsx` |
| calendar | 公司行事曆 | `Calendar.tsx` |
| uom | 單位量測管理 | `UomManagement.tsx` |
| org | 組織架構 | `OrganizationManagement.tsx` |
| costcenter | 成本中心管理 | `CostCenterManagement.tsx` |
| substitute | 替代料管理 | `SubstituteManagement.tsx` |
| mrp | MRP 物料需求計劃 | `MrpPlanning.tsx` |
| aps | APS 有限產能排程 | `ApsScheduling.tsx` |
| engineering | 工藝設計 | `ManufacturingEngineering.tsx` |
| mps | MPS 主生產排程 | `MpsPlanning.tsx` |
| tpm | 設備維護 TPM | `TpmMaintenance.tsx` |
| barcode | 條碼系統 | `BarcodeSystem.tsx` |
| kpi | KPI 績效看板 | `KpiDashboard.tsx` |
| supplierportal | 供應商入口 | `SupplierPortal.tsx` |
| aischeduling | AI 智能排班 | `AiScheduling.tsx` |
| aidecision | AI 決策助手 | `AiDecision.tsx` |
| profile | 個人設定 | `profile/ProfileSettings.tsx` |
| systemsettings | 系統設定 | `SystemSettings.tsx` |

---

## 三、頁面詳細說明

### 3.1 儀表板 (Dashboard)

**路由**: `/dashboard` (首頁)

**主要功能**:
- KPI 卡片：總員工數、今日出勤、待審批件數、活躍警示數
- 生產趨勢圖 (Area Chart)
- 考勤趨勢圖 (Bar Chart)
- 工單進度列表
- 月曆小工具
- 快捷工具箱

---

### 3.2 銷貨及收款 (Sales)

**路由**: `/sales`

**主要功能**:
- 銷售訂單管理 (建立、編輯、狀態更新)
- 客戶管理
- 報價單管理與轉訂單
- 出貨管理
- 發票管理
- 帳齡分析

**訂單狀態**: draft → confirmed → in_production → shipped → partial → invoiced → completed → cancelled

---

### 3.3 採購及付款 (Procurement)

**路由**: `/procurement`

**主要功能**:
- 採購訂單管理 (建立、編輯、鎖定/解鎖)
- 供應商管理 (含供應商入口網站用戶)
- 請購單 (PR)
- RFQ 報價請求
- 進貨驗收單 (Goods Receipt)
- 三單匹配 (Three-Way Match: PO/GR/Invoice)

---

### 3.4 庫存管理 (Inventory)

**路由**: `/inventory`

**主要功能**:
- 品號 (SKU) 管理
  - 基本資訊 (品號、品名、規格)
  - 來源類型 (自製/採購/委外)
  - ABC 分類、安全庫存、再訂購點
  - 售價與成本
  - 單位轉換
- 倉庫管理
- 儲位管理
- 庫存交易記錄
- 低庫存警示
- 調撥單管理
- 完工入庫單

---

### 3.5 生產製造 (Manufacturing)

**路由**: `/manufacturing`

**主要功能**:
- 工單管理
  - 建立工單 (一般/重工/樣品/維修)
  - 派工管理
  - 狀態追蹤
  - 優先度設定
- 工單進度追蹤

**工單狀態**: pending → released → in_progress → completed → closed

---

### 3.6 人力資源 (HR)

**路由**: `/hr`

**主要功能**:
- 員工管理
  - 身分資料與聯絡方式
  - 組織職位 (部門、職稱)
  - 薪資金融
  - 法定保障 (勞健保)
- 考勤管理
- 請假申請與核決
- 薪資結算

---

### 3.7 財務管理 (Finance)

**路由**: `/finance`

**主要功能**:
- 借款管理
- 銀行帳戶維護（需財務檢視權限；新增需建立權限，編輯／啟用／停用需編輯權限）
- 投資部位管理
- 擔保品管理
- 應付帳款
- 三單匹配

**銀行帳戶維護**:
- 於「銀行帳戶維護」分頁新增或編輯銀行名稱、分行、帳號、帳戶名稱、幣別及收款總帳科目。
- 收款總帳科目僅能選擇啟用中的現金／銀行科目；未設定有效科目的帳戶不會出現在「登錄收款」選單。
- 可停用或重新啟用帳戶；停用不會刪除帳戶或歷史資料。
- 帳務餘額僅供檢視，不在此頁調整。

---

### 3.8 固定資產 (Assets)

**路由**: `/assets`

**主要功能**:
- 資產登記 (編號、名稱、成本、耐用年限)
- 類別管理
- 狀態追蹤 (active/idle/maintenance/disposed)
- 折舊提列
- 維護與轉移記錄

---

### 3.9 預算規劃 (Budget)

**路由**: `/budget`

**主要功能**:
- 預算年度管理
- 版本控制 (多版本)
- 費用預算
- 人員預算
- 資本支出預算 (CAPEX)

---

### 3.10 品質管理 (Quality)

**路由**: `/quality`

**主要功能**:
- IQC 進料檢驗
- IPQC 製程檢驗
- OQC 出貨檢驗
- NCR (異常單)
- 供應商客訴
- CAPA (改善措施)
- Lot 追溯查詢

---

### 3.11 研發管理 (RD)

**路由**: `/rd`

**主要功能**:
- 研發專案管理
- 技術文件管理
- BOM 管理 (樹狀檢視、成本滾動)
- ECN 變更管理
- 試產管理
- 量產轉換

---

### 3.12 ESG 永續管理 (ESG)

**路由**: `/esg`

**主要功能**:
- 碳排放記錄 (Scope 1/2/3)
- 排放源管理
- 排放因子維護
- 減排目標設定
- 能源消耗監控
- 供應商永續性評估
- AI 洞察分析

---

### 3.13 AI Agent 監控 (AI)

**路由**: `/ai`

**主要功能**:
- Agent 效能監控儀表板
- 延遲/QPS/錯誤率圖表
- 系統拓撲圖
- Flow 執行歷史
- **AI 規劃測試區** (建立規劃、執行規劃、工具測試)

---

### 3.14 MRP 物料需求計劃 (MrpPlanning)

**路由**: `/mrp`

**主要功能**:
- MRP 執行記錄
- 需求檢視
- 計劃訂單
- MRP 參數配置

---

### 3.15 APS 有限產能排程 (ApsScheduling)

**路由**: `/aps`

**主要功能**:
- 生產排程管理
- Gantt 圖檢視
- 產能負載檢視
- 約束條件管理
- 排程優化

---

### 3.16 設備維護 TPM (TpmMaintenance)

**路由**: `/tpm`

**主要功能**:
- 設備管理
- 維修記錄
- 停機記錄
- 預防保養排程

---

### 3.17 條碼系統 (BarcodeSystem)

**路由**: `/barcode`

**主要功能**:
- 條碼掃描
- 條碼產生 (品項/批號/庫位/箱號/棧板)
- 掃描歷史記錄

---

### 3.18 KPI 績效看板 (KpiDashboard)

**路由**: `/kpi`

**主要功能**:
- OEE 設備效能
- 準時交貨率 (OTD)
- 在製品數量 (WIP)
- 報廢率
- 產能利用率

---

### 3.19 組織架構管理 (OrganizationManagement)

**路由**: `/org`

**主要功能**:
- 部門管理 (樹狀結構)
- 職等管理
- 職稱管理
- 核決權限

---

### 3.20 系統設定 (SystemSettings)

**路由**: `/systemsettings`

**主要功能**:
- 權限管理
  - 權限樹狀結構
  - 使用者角色管理
- 主題設定

---

### 3.21 公司行事曆 (Calendar)

**路由**: `/calendar`

**主要功能**:
- 月/週/日檢視
- 事件 CRUD
- 事件類別管理
- 會議室管理
- 公司假日

---

## 四、共用組件

| 組件 | 用途 |
|------|------|
| `Sidebar` | 側邊導航欄 |
| `Topbar` | 頂部標題欄 |
| `FormModal` | 表單彈窗 |
| `DataTable` | 資料表格 |
| `ConfirmDialog` | 確認對話框 |
| `BomTreeModal` | BOM 樹狀檢視 |
| `Warehouse2DViewer` | 倉庫 2D 檢視 |
| `Warehouse3DViewer` | 倉庫 3D 檢視 |

---

## 五、API 錯誤處理

| 錯誤碼 | 處理方式 |
|--------|----------|
| 無回應 | 顯示「網路連線錯誤」 |
| 401 | 清除 Token，重新導向登入頁 |
| 400 | 顯示驗證失敗訊息 |
| 403 | 顯示「沒有權限」 |
| 404 | 顯示「資源不存在」 |
| 5xx | 顯示「伺服器內部錯誤」 |

---

## 六、开发环境

| 項目 | 值 |
|------|-----|
| API 伺服器 | http://localhost:5100 |
| 前端端口 | 5173 (Vite) |
| 開發框架 | React 18 |
| 建置命令 | `npm run dev` |

---

## 七、相關檔案路徑

| 類別 | 檔案路徑 |
|------|----------|
| 頁面 | `erp-frontend/src/pages/*.tsx` |
| 組件 | `erp-frontend/src/components/*.tsx` |
| API 定義 | `erp-frontend/src/api/api.ts` |
| Hooks | `erp-frontend/src/hooks/useApi.ts` |
| 路由設定 | `erp-frontend/src/App.tsx` |
| 型別定義 | `erp-frontend/src/types/index.ts` |
| 樣式 | `erp-frontend/src/index.css` |

---

*操作手冊版本: 1.0*
*更新日期: 2026-04-13*