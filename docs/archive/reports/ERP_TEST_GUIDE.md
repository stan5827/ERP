# ERP 系統功能完整測試指南

## 一、系統概覽

### 系統資訊
| 項目 | 內容 |
|------|------|
| 系統名稱 | 製造業資源規劃系統 (ERP) |
| 後端 | .NET 8 + Entity Framework + SQL Server |
| 前端 | React + TypeScript + Vite |
| 後端網址 | http://localhost:5100 |
| 前端網址 | http://localhost:5173 |

### 登入帳號
- **帳號**: `admin`
- **密碼**: `admin123`

---

## 二、後端 API 清單

### 2.1 核心業務 API

| 模組 | Endpoint | 說明 |
|------|----------|------|
| 銷貨 | `/api/sales-orders` | 銷售訂單管理 |
| 採購 | `/api/purchase-orders` | 採購訂單 |
| 採購 | `/api/purchase-requisitions` | 採購需求 |
| 庫存 | `/api/inventory` | 庫存管理 |
| 庫存 | `/api/skus` | 料號管理 |
| 庫存 | `/api/warehouses` | 倉庫管理 |
| 庫存 | `/api/bin-locations` | 儲位管理 |
| 生產 | `/api/work-orders` | 工單管理 |
| 生產 | `/api/wip-reports` | WIP 報工 |
| 品質 | `/api/quality` | 品質檢驗 |

### 2.2 支援功能 API

| 模組 | Endpoint | 說明 |
|------|----------|------|
| HR | `/api/employees` | 員工管理 |
| HR | `/api/attendance` | 出勤打卡 |
| HR | `/api/leave-requests` | 請假申請 |
| HR | `/api/payroll` | 薪資管理 |
| 財務 | `/api/finance` | 財務總帳 |
| 財務 | `/api/accounts-payable` | 應付款 |
| 財務 | `/api/cash-receipts` | 收款管理 |
| 資產 | `/api/assets` | 固定資產 |
| 預算 | `/api/budget` | 預算規劃 |
| 研發 | `/api/rd` | 研發管理 |
| 研發 | `/api/boms` | BOM 表 |

### 2.3 計劃與排程 API

| 模組 | Endpoint | 說明 |
|------|----------|------|
| MRP | `/api/mrp` | 物料需求計劃 |
| APS | `/api/aps` | 有限產能排程 |
| MPS | `/api/mps` | 主生產排程 |
| TPM | `/api/equipment` | 設備維護 |
| 條碼 | `/api/barcode` | 條碼系統 |
| 工藝 | `/api/routings` | 工藝路線 |
| 工藝 | `/api/operations` | 工序管理 |
| 工藝 | `/api/work-centers` | 工作中心 |

### 2.4 數位轉型 API

| 模組 | Endpoint | 說明 |
|------|----------|------|
| ESG | `/api/esg` | ESG 永續管理 |
| 碳排放 | `/api/carbon` | 碳排放儀表板 |
| AI | `/api/ai` | AI 排班 |
| AI | `/api/ai/schedule/*` | AI 智能排班 |
| AI 決策 | `/api/aidecision` | AI 決策助手 |
| 供應商 | `/api/supplier-portal/*` | 供應商入口 |
| EDI | `/api/edi` | EDI 匯出 |
| Agent | `/api/agent` | AI Agent 監控 |

### 2.5 系統管理 API

| 模組 | Endpoint | 說明 |
|------|----------|------|
| 登入 | `/api/auth/login` | 帳號登入 |
| 用戶 | `/api/users` | 用戶管理 |
| 角色 | `/api/roles` | 角色權限 |
| 組織 | `/api/departments` | 部門組織 |
| 組織 | `/api/job-titles` | 職稱管理 |
| 組織 | `/api/job-grades` | 職等管理 |
| 單位 | `/api/uoms` | 單位量測 |
| 成本 | `/api/cost-centers` | 成本中心 |
| 審批 | `/api/approvals` | 審批流程 |
| 日曆 | `/api/calendar` | 行事曆 |
| 協作 | `/api/collaboration` | 協作平台 |
| KPI | `/api/kpi` | 績效指標 |

---

## 三、前端頁面對應

| 側邊欄選單 | 頁面檔案 | 路由 |
|-----------|----------|------|
| 儀表板 | Dashboard.tsx | dashboard |
| 銷貨管理 | Sales.tsx | sales |
| 採購管理 | Procurement.tsx | procurement |
| 庫存管理 | Inventory.tsx | inventory |
| 生產製造 | Manufacturing.tsx | manufacturing |
| 品質管理 | Quality.tsx | quality |
| 人力資源 | HR.tsx | hr |
| 財務管理 | Finance.tsx | finance |
| 固定資產 | Assets.tsx | assets |
| 預算規劃 | Budget.tsx | budget |
| 研發管理 | RD.tsx | rd |
| ESG 永續 | ESG.tsx | esg |
| AI 監控中心 | AI.tsx | ai |
| Agent Flow | AgentFlows.tsx | agentflows |
| 協作平台 | Collaboration.tsx | collaboration |
| 行事曆 | Calendar.tsx | calendar |
| 單位量測 | UomManagement.tsx | uom |
| 組織架構 | OrganizationManagement.tsx | org |
| 成本中心 | CostCenterManagement.tsx | costcenter |
| 替代料管理 | SubstituteManagement.tsx | substitute |
| MRP 物料需求 | MrpPlanning.tsx | mrp |
| APS 有限產能 | ApsScheduling.tsx | aps |
| MPS 主生產排程 | MpsPlanning.tsx | mps |
| 設備維護 TPM | TpmMaintenance.tsx | tpm |
| 條碼系統 | BarcodeSystem.tsx | barcode |
| 工藝設計 | ManufacturingEngineering.tsx | engineering |
| KPI 績效看板 | KpiDashboard.tsx | kpi |
| 供應商入口 | SupplierPortal.tsx | supplierportal |
| AI 智能排班 | AiScheduling.tsx | aischeduling |
| AI 決策助手 | AiDecision.tsx | aidecision |
| EDI 匯出 | EdiExport.tsx | ediexport |

---

## 四、測試案例

### 4.1 登入測試
```
POST /api/auth/login
Body: { "username": "admin", "password": "admin123" }
預期: 回傳 JWT token
```

### 4.2 API 認證方式
```
Header: Authorization: Bearer <token>
```

### 4.3 功能模組測試清單

#### A. 核心業務
- [ ] 銷貨訂單建立與查詢
- [ ] 採購訂單建立與審批
- [ ] 庫存進出入庫操作
- [ ] 工單建立與報工
- [ ] 品質檢驗記錄

#### B. 支援功能
- [ ] 員工資料管理
- [ ] 出勤打卡
- [ ] 請假申請審批
- [ ] 財務傳票建立
- [ ] 固定資產折舊
- [ ] 預算編列
- [ ] BOM 物料清單

#### C. 計劃與排程
- [ ] MRP 運行與結果檢視
- [ ] APS 甘特圖排程
- [ ] MPS 主排程建立
- [ ] 設備維護記錄
- [ ] 條碼掃描操作
- [ ] 工藝路線設定

#### D. 數位轉型
- [ ] ESG 排放紀錄
- [ ] 碳排放儀表板
- [ ] AI 智能排班產生
- [ ] AI 決策助手 (需求預測/採購建議)
- [ ] 供應商登入與報價
- [ ] EDI 資料匯出
- [ ] AI Agent 監控

#### E. 系統管理
- [ ] 用戶權限設定
- [ ] 組織架構設定
- [ ] 審批流程設定
- [ ] 行事曆建立
- [ ] KPI 儀表板檢視

---

## 五、測試工具推薦

### 5.1 API 測試工具
- **Postman** - 適合複雜 API 測試
- **Insomnia** - 輕量級 API 客戶端
- **curl** - 命令列快速測試

### 5.2 瀏覽器開發者工具
- **Network 面板** - 檢視 API 請求
- **Console 面板** - 檢視錯誤訊息
- **Application 面板** - 檢視 localStorage

---

## 六、快速測試指令

### 6.1 取得 Token
```bash
curl -X POST http://localhost:5100/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### 6.2 測試各模組 API
```bash
# 設定 TOKEN
TOKEN="your-jwt-token-here"

# 測試庫存
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5100/api/skus

# 測試 KPI
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5100/api/kpi/dashboard?fromDate=2026-01-01&toDate=2026-04-09"

# 測試 AI 排班
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5100/api/ai/schedule/risks

# 測試 AI 決策
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5100/api/aidecision/demand-forecast
```

---

## 七、常見問題排查

| 問題 | 可能原因 | 解決方式 |
|------|----------|----------|
| 登入失敗 | 帳密錯誤 | 確認 admin/admin123 |
| 401 錯誤 | Token 過期 | 重新登入取得新 token |
| 500 錯誤 | 伺服器錯誤 | 檢查後端日誌 |
| 空白數據 | 新系統無資料 | 這是正常現象 |
| 頁面載入失敗 | 前端編譯錯誤 | 檢查瀏覽器 Console |

---

## 八、系統建置狀態

| 項目 | 狀態 |
|------|------|
| 後端編譯 | ✅ 成功 |
| 前端編譯 | ✅ 成功 |
| 後端執行 | ✅ 運行中 (port 5100) |
| 前端執行 | ✅ 運行中 (port 5173) |
| API 回應 | ✅ 正常 |

---

## 九、系統功能總覽

### 後端 Controller 數量: 69 個
### 前端 Page 數量: 32 個

### 已實作模組
- 銷貨管理 (Sales Orders)
- 採購管理 (Purchase Orders/Requisitions)
- 庫存管理 (Inventory/SKUs/Warehouses/Bins)
- 生產製造 (Work Orders/WIP Reports)
- 品質管理 (Quality Inspection)
- 人力資源 (Employees/Attendance/Payroll)
- 財務管理 (Finance/AP/AR)
- 固定資產 (Assets)
- 預算規劃 (Budget)
- 研發管理 (R&D/BOMs)
- ESG 永續管理
- 碳排放儀表板
- AI 智能排班
- AI 決策助手
- 供應商入口網站
- EDI 整合
- MRP 物料需求計劃
- APS 有限產能排程
- MPS 主生產排程
- TPM 設備維護
- 條碼系統
- 工藝設計
- KPI 績效看板

---

*最後更新: 2026-04-09*
*版本: v1.0*
