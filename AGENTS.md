# AGENTS.md — Nexus ERP 企業資源規劃系統

> 專案根目錄：D:\ERP  
> 最後更新：2026-06-15  
> 版本：v2.0

---

## 專案概述

本專案是一套面向**製造業**的全方位企業資源規劃（ERP）系統，名為 **Nexus ERP**。  
系統涵蓋 11 大核心模組，旨在消除資訊孤島、建立單一資料來源（Single Source of Truth），並提升端到端流程效率。

### 核心業務模組

| 編號 | 模組 | 說明 |
|------|------|------|
| 1 | 儀表板 (Dashboard) | 高階 KPI 監控與即時營運指標 |
| 2 | 銷貨及收款 (Sales) | 客戶管理、報價單、銷售訂單、出貨、發票、收款 |
| 3 | 採購及付款 (Procurement) | 供應商管理、詢報價 (RFQ)、請購單 (PR)、採購單 (PO)、收料 (GR) |
| 4 | 庫存管理 (Inventory) | SKU 主檔、多倉儲、批號/序號管控、盤點、補貨、調撥 |
| 5 | 生產製造 (MES) | 工單 (WO)、途程 (Routing)、報工 (WIP)、報廢、設備維護 (TPM) |
| 6 | 物料需求計畫 (MRP) | 需求展開、淨需求計算、建議採購/生產單 |
| 7 | 進階排程系統 (APS) | 產能負荷評估、最佳化排程、產線派工 |
| 8 | 主生產排程 (MPS/RCCP) | 粗略產能規劃、時格排程、資源用量校準 |
| 9 | 研發與工程 (R&D) | BOM 管理、ECR/ECN 工程變更、產品規格書、實驗日誌 |
| 10 | 品質保證 (QA) | IQC 進料檢驗、IPQC 製程品檢、FQC/OQC 成品驗收、品質追溯 |
| 11 | 財務與成本 (Finance) | 實際成本結算、應收/應付帳款、資產折舊、預算控管 |
| 12 | 人力資源 (HR) | 員工主檔、出勤排班、請假加班、薪資核算、績效培訓 |
| 13 | 固定資產 (Assets) | 資產增添/移轉/報廢、折舊計算、維護修繕紀錄 |
| 14 | 預算規劃 (Budget) | 銷售預測、原料成本預測、人力預算、部門費用控管 |
| 15 | 融資及投資 (Treasury) | 借款管理、資金調度、保證承兌、長短期投資 |
| 16 | ESG 永續管理 | 溫室氣體盤查、碳足跡計算、排放係數管理 |
| 17 | AI 智能助手 | 決策指標分析、財務異常偵測、自然語言查詢 |
| 18 | 供應商協作平台 (Supplier Portal) | 供應商線上報價、訂單確認、ASN、發票請款 |
| 19 | 內部網站與協作 (Intranet) | 公告訊息、跨部門交辦事項、會議室預約 |
| 20 | 系統設定 (System) | 使用者/角色/權限、審計日誌、系統排程、通知 |

### 跨模組核心資料流

- **報價到收款 (Quote-to-Cash)：** 報價 → 銷售訂單 → 出貨 → 發票 → 收款
- **採購到付款 (Procure-to-Pay)：** 請購 → RFQ → 採購單 → 收料/IQC → 入庫 → 發票 → 付款
- **計畫到生產 (Plan-to-Produce)：** MRP 展開 → 工單排程 → 發料 → 生產報工 → 完工入庫 → 成本結算

---

## 技術架構

### 整體架構

`
┌─────────────────────────────────────────────────┐
│                 客戶端層 (Client Layer)            │
│  ┌──────────────┐  ┌──────────────┐  ┌────────┐ │
│  │ React Web    │  │ Expo Mobile  │  │Supplier │ │
│  │ (erp-frontend)│  │(erp-employee)│  │ Portal  │ │
│  └──────┬───────┘  └──────┬───────┘  └────┬───┘ │
│         │                  │                │      │
└─────────┼──────────────────┼────────────────┼──────┘
          │                  │                │
┌─────────▼──────────────────▼────────────────▼──────┐
│              API Gateway (ErpGateway)                │
│         YARP Reverse Proxy + JWT 驗證                │
│         OpenTelemetry 分散式追蹤                      │
└──────────────────────┬─────────────────────────────┘
                       │
┌──────────────────────▼─────────────────────────────┐
│              Backend API (ErpBackend)                │
│  ASP.NET Core 8.0 Web API + EF Core + PostgreSQL    │
│  MassTransit 事件匯流排  │  Hangfire 背景排程        │
│  Prometheus 指標收集    │  Serilog 集中式日誌        │
└────────────────────────────────────────────────────┘
                       │
┌──────────────────────▼─────────────────────────────┐
│              基礎設施層 (Infrastructure)              │
│  PostgreSQL │ Redis 快取 │ Prometheus │ Grafana      │
└────────────────────────────────────────────────────┘
`

### 前端 — Web 管理介面

| 項目 | 說明 |
|------|------|
| **路徑** | D:\ERP\erp-frontend |
| **框架** | React 19 + TypeScript + Vite 8 |
| **UI 元件** | Radix UI + Tailwind CSS + Framer Motion + Lucide Icons |
| **狀態管理** | TanStack React Query (資料取用) + React Context (角色/主題) |
| **路由** | React Router DOM v7 |
| **HTTP 客戶端** | Axios (含攔截器處理统一 ApiResponse 格式) |
| **圖表** | Recharts |
| **驗證** | Zod (表單與 API 資料驗證) |
| **E2E 測試** | Playwright (chromium) |
| **ESLint** | typescript-eslint strict mode |
| **開發伺服器** | 
pm run dev → http://localhost:5173 |
| **API Proxy** | /api → http://localhost:5100 |
| **主要頁面** | Dashboard, Sales, Procurement, Inventory, Manufacturing, HR, Finance, Quality, RD, Assets, Budget, ESG, AI, Supplier Portal, Collaboration, Calendar, System Settings |

### 前端 — 員工行動 App

| 項目 | 說明 |
|------|------|
| **路徑** | D:\ERP\erp-employee-app |
| **框架** | Expo (React Native) + TypeScript |
| **功能** | 員工出勤打卡、請假申請、薪資查詢、通知推送 |
| **開發伺服器** | 
pm start |

### 後端 — API 服務

| 項目 | 說明 |
|------|------|
| **路徑** | D:\ERP\ErpBackend |
| **框架** | ASP.NET Core 8.0 Web API |
| **ORM** | Entity Framework Core 8 + Npgsql (PostgreSQL) |
| **認證** | JWT Bearer (BCrypt.Net-Next 密碼雜湊) |
| **API 風格** | RESTful，路由使用 Kebab-Case |
| **統一回應格式** | { success, message, data } / { success, message, errors } |
| **API 版本** | /api/v1/erp/... |
| **文件** | Swagger / OpenAPI (Swashbuckle) |
| **背景任務** | Hangfire |
| **事件匯流排** | MassTransit |
| **快取** | Redis (StackExchange.Redis) + MemoryCache |
| **監控** | Prometheus + OpenTelemetry + Serilog |
| **稽核** | AuditInterceptor (EF Core) + AuditLogFilter (MVC) |
| **權限** | RBAC (Role-Based Access Control) + PermissionAuthorize Filter |
| **開發伺服器** | dotnet run → http://localhost:5100 |

### 閘道層 — API Gateway

| 項目 | 說明 |
|------|------|
| **路徑** | D:\ERP\ErpGateway |
| **框架** | ASP.NET Core 8.0 + YARP (Yet Another Reverse Proxy) |
| **功能** | JWT 驗證、CORS、分散式追蹤 (OpenTelemetry) |

### 資料庫

| 項目 | 說明 |
|------|------|
| **RDBMS** | PostgreSQL |
| **連線字串** | 環境變數 DB_CONNECTION_STRING 或 ppsettings.json → ConnectionStrings.ErpGeneral |
| **Migration** | EF Core Migrations (D:\ERP\ErpBackend\Migrations\) |
| **全域欄位** | 所有交易表含 CreatedAt, UpdatedAt, CreatedBy, ModifiedBy, Status |
| **不可變原則** | InventoryTransactions, ActualCostings 等關鍵表一經寫入不得修改，僅能反向沖銷 |

### 監控與觀察性

| 項目 | 說明 |
|------|------|
| **路徑** | D:\ERP\monitoring |
| **Prometheus** | 收集 API QPS、Latency、5xx 錯誤率等指標 |
| **Grafana** | 儀表板視覺化 (Enterprise 版) |
| **OpenTelemetry** | 分散式追蹤 (Gateway → Backend) |
| **Docker Compose** | monitoring/docker-compose.yml |

---

## 專案結構

`
D:\ERP/
├── ERP.sln                              # 解決方案 (5 個專案)
├── ErpBackend/                          # 後端 API (ASP.NET Core 8.0)
│   ├── Controllers/                     # 60+ 個 Controller (各模組 API)
│   ├── Models/                          # EF Core 資料模型 (15 個模組分類)
│   │   ├── System/                      # 使用者、角色、權限、稽核
│   │   ├── HR/                          # 部門、員工、出勤、薪資
│   │   ├── Inventory/                   # SKU、倉庫、儲位、庫存異動
│   │   ├── MES/                         # 工單、產線、MRP、APS、KPI
│   │   ├── Sales/                       # 報價、訂單、出貨、發票
│   │   ├── Procurement/                 # 請購、採購單、供應商
│   │   ├── Finance/                     # 帳款、成本、預算
│   │   ├── QA/                          # 檢驗報告、品質追溯
│   │   ├── RD/                          # BOM、ECN、ECR
│   │   ├── Asset/                       # 固定資產、折舊
│   │   ├── Budget/                      # 預算版本、控管
│   │   ├── ESG/                         # 碳排、永續
│   │   ├── Collaboration/               # 公告、協作
│   │   ├── Digital/                     # 數位轉型
│   │   └── Portal/                      # 供應商入口
│   ├── Services/                        # 業務邏輯服務層
│   ├── Data/                            # DbContext, AuditInterceptor
│   ├── Migrations/                      # EF Core Migration
│   ├── Seeders/                         # 初始資料 (UOM)
│   ├── Configs/                         # AI/LLM 組態
│   ├── Sagas/                           # 訂單狀態機
│   ├── Consumers/                       # MassTransit 消費者
│   ├── Events/                          # 領域事件
│   ├── Filters/                         # MVC 過濾器 (稽核、驗證)
│   ├── Middleware/                      # 自訂 Middleware
│   ├── Validation/                      # 驗證規則
│   ├── Program.cs                       # 應用程式入口
│   └── appsettings.json                 # 組態 (環境變數注入)
├── ErpGateway/                          # API 閘道 (YARP Reverse Proxy)
├── ErpBackend.Tests/                    # 單元測試
├── ErpBackend.IntegrationTests/         # 整合測試
├── seed-admin/                          # 管理員初始資料種子工具
├── erp-frontend/                        # React 前端 Web 應用程式
├── erp-employee-app/                    # Expo 行動應用程式
├── erp-remotion-video/                  # Remotion 影片生成 (內部工具)
├── monitoring/                          # Prometheus + Grafana 監控
├── scripts/                             # 維護腳本與修復工具
├── tools/                               # 工具程式 (對話分析器等)
├── docs/                                # 專案文件 (見下方)
├── .github/workflows/ci.yml             # GitHub Actions CI/CD
├── .vscode/settings.json                # VS Code 設定
├── start-all.ps1                        # 啟動所有服務腳本
├── test-all.ps1                         # 執行所有測試腳本
├── seed-admin.ps1                       # 初始化管理員帳號
└── 操作手冊_新增品號.docx               # 操作文件
`

---

## 文件索引

### 專案規劃與架構

| 文件 | 說明 |
|------|------|
| [docs/01_Project_Charter_and_Plan.md](D:\ERP\docs\01_Project_Charter_and_Plan.md) | 專案章程、階段時程 (16 週)、風險管理 |
| [docs/02_Business_Blueprint.md](D:\ERP\docs\02_Business_Blueprint.md) | 業務藍圖、11 大模組概覽、跨模組資料流 |
| [docs/03_Functional_Requirement_Document.md](D:\ERP\docs\03_Functional_Requirement_Document.md) | 各模組功能需求與業務規則 |
| [docs/04_System_Architecture_and_ERD.md](D:\ERP\docs\04_System_Architecture_and_ERD.md) | 系統架構、ERD、多幣別設計、審計軌跡 |
| [docs/05_API_Specifications.md](D:\ERP\docs\05_API_Specifications.md) | API 設計規範、統一回應格式、版本控管 |
| [docs/06_Test_Plan_and_Cases.md](D:\ERP\docs\06_Test_Plan_and_Cases.md) | 測試計畫與案例 |
| [docs/07_Go_Live_and_Migration_Plan.md](D:\ERP\docs\07_Go_Live_and_Migration_Plan.md) | 上線與資料遷移計畫 |
| [docs/08_Refactoring_and_Optimization_Roadmap.md](D:\ERP\docs\08_Refactoring_and_Optimization_Roadmap.md) | 重構與優化路線圖 |
| [docs/09_ERP_Transformation_Blueprint.md](D:\ERP\docs\09_ERP_Transformation_Blueprint.md) | ERP 改造藍圖 (12-18 個月) |

### 模組操作手冊

| 文件 | 說明 |
|------|------|
| [docs/各部門ERP系統操作手冊_完整版.md](D:\ERP\docs\各部門ERP系統操作手冊_完整版.md) | 供應商協作平台完整操作手冊 |
| [docs/ERP前端手冊.md](D:\ERP\docs\ERP前端手冊.md) | 前端架構、路由、模組列表 |
| [docs/ERP核心表單欄位標準.md](D:\ERP\docs\ERP核心表單欄位標準.md) | 全域表單欄位命名與驗證標準 |
| [docs/庫存管理總手冊.md](D:\ERP\docs\庫存管理總手冊.md) | 入庫、出庫、盤點、補貨流程 |
| [docs/採購及供應商協作手冊.md](D:\ERP\docs\採購及供應商協作手冊.md) | PR→PO→GR→AP 閉環流程 |
| [docs/生產及製造循環.md](D:\ERP\docs\生產及製造循環.md) | MES + QA 核心表單與流程 |
| [docs/銷貨及收款手冊.md](D:\ERP\docs\銷貨及收款手冊.md) | 報價→訂單→出貨→發票→收款 |
| [docs/BOM管理手冊.md](D:\ERP\docs\BOM管理手冊.md) | BOM 維護、ECN、版本控管 |
| [docs/HR管理手冊.md](D:\ERP\docs\HR管理手冊.md) | 員工主檔、出勤、薪資、離職 |
| [docs/MPS_RCCP_與多幣別管理手冊.md](D:\ERP\docs\MPS_RCCP_與多幣別管理手冊.md) | 主生產排程、產能規劃、匯率管理 |
| [docs/固定資產循環.md](D:\ERP\docs\固定資產循環.md) | 資產增添/移轉/報廢/折舊 |
| [docs/研發循環.md](D:\ERP\docs\研發循環.md) | 研發專案、產品規格、ECN/ECR、DMS |
| [docs/薪資核算.md](D:\ERP\docs\薪資核算.md) | 台灣勞基法薪資結構、勞健保、扣繳 |
| [docs/預算規劃循環.md](D:\ERP\docs\預算規劃循環.md) | 銷售預測、原料成本、人力預算 |
| [docs/融資及投資循環.md](D:\ERP\docs\融資及投資循環.md) | 借款管理、資金調度、保證承兌、投資 |
| [docs/內部網站與協作.md](D:\ERP\docs\內部網站與協作.md) | 公告、跨部門協作、會議室預約 |
| [docs/數位轉型新趨勢模組.md](D:\ERP\docs\數位轉型新趨勢模組.md) | ESG 碳盤查、AI 智能助手 |
| [docs/AI_Planning_Agent_Spec.md](D:\ERP\docs\AI_Planning_Agent_Spec.md) | AI 規劃代理系統規格書 (LangGraph) |
| [docs/AI_Planning_Agent_操作手冊.md](D:\ERP\docs\AI_Planning_Agent_操作手冊.md) | AI Planning Agent 操作指南 |
| [docs/ERP_修復規劃_2026-05-03.md](D:\ERP\docs\ERP_修復規劃_2026-05-03.md) | 系統修復規劃記錄 |

### 表單指南

| 文件 | 說明 |
|------|------|
| [docs/form_guide/README.md](D:\ERP\docs\form_guide\README.md) | 表單功能說明指南入口 |
| [docs/form_guide/hr_forms.md](D:\ERP\docs\form_guide\hr_forms.md) | HR 表單說明 |
| [docs/form_guide/inventory_forms.md](D:\ERP\docs\form_guide\inventory_forms.md) | 庫存表單說明 |
| [docs/form_guide/procurement_forms.md](D:\ERP\docs\form_guide\procurement_forms.md) | 採購表單說明 |
| [docs/form_guide/manufacturing_forms.md](D:\ERP\docs\form_guide/manufacturing_forms.md) | 生產表單說明 |
| [docs/form_guide/manufacturing_engineering_forms.md](D:\ERP\docs\form_guide/manufacturing_engineering_forms.md) | 製程工程表單說明 |
| [docs/form_guide/budget_forms.md](D:\ERP\docs\form_guide/budget_forms.md) | 預算表單說明 |
| [docs/form_guide/assets_forms.md](D:\ERP\docs\form_guide/assets_forms.md) | 資產表單說明 |

### 歸檔文件

| 目錄 | 說明 |
|------|------|
| [docs/archive/](D:\ERP\docs\archive) | 舊版文件、歷史報告、已合併內容 |

---

## 開發規範

### 後端 (C# / ASP.NET Core)

- 遵循 .NET 8 官方編碼慣例
- 使用 EF Core Code-First 模式進行資料庫設計
- 所有 Controller 繼承 BaseController，使用統一 ApiResponse 格式
- 路由採用 Kebab-Case 命名慣例（內建 KebabCaseParameterTransformer）
- 密碼使用 BCrypt.Net-Next 雜湊，永不儲存明文
- 所有 CRUD 操作自動透過 AuditLogFilter 記錄審計日誌
- 使用 [PermissionAuthorize] 屬性進行 RBAC 權限控管
- 服務層 (Services) 封裝業務邏輯，Controller 僅負責 HTTP 層處理
- 使用 MassTransit 處理异步事件通訊
- 使用 Hangfire 處理背景排程任務
- 資料庫連線字串透過環境變數 DB_CONNECTION_STRING 注入
- JWT 金鑰至少 32 字元，透過 JWT_SECRET_KEY 環境變數設定

### 前端 (React / TypeScript)

- 使用严格模式 TypeScript (strict: true)
- 避免使用 ny 類型，使用 interface 定義 DTO
- 所有 API 呼叫透過 pi.ts / aseApi.ts 統一發送
- 使用 Zod 進行表單與 API 資料驗證
- 使用 TanStack Query 管理伺服器狀態與快取
- 使用 Framer Motion 處理動畫過渡
- 使用 React.lazy + Suspense 進行路由級程式碼分割
- ESLint strict mode 檢查
- 元件放置於 components/，頁面放置於 pages/

### 資料庫規範

- 所有交易表必須包含：Id, CreatedAt, UpdatedAt, CreatedBy, ModifiedBy, Status
- 關鍵交易表（庫存異動、實際成本）遵循**不可變原則**，僅能反向沖銷
- 多幣別支援：單據明細同時記錄「原幣金額」與「本位幣金額」
- 稅務欄位：TaxCode, TaxRate, IsTaxInclusive, InvoiceNumber
- 匯率表：記錄 FromCurrency, ToCurrency, Rate, ValidFrom, ValidTo, RateType, Source

### 安全規範

- JWT Authentication + RBAC Authorization
- 密碼使用 bcrypt 雜湊，強制首次登入修改密碼
- 密碼歷史記錄（PasswordHistory）防止重複使用
- 所有 API 操作記錄審計日誌（AuditLog）
- CORS 僅允許預先定義的前端來源
- API Gateway 層進行 JWT 驗證

### 測試

- 後端單元測試：D:\ERP\ErpBackend.Tests
- 後端整合測試：D:\ERP\ErpBackend.IntegrationTests
- 前端 E2E 測試：Playwright (chromium)
- CI/CD：GitHub Actions (.github/workflows/ci.yml)

### Git 工作流

- Feature Branch 策略：eature/*, ugfix/*, hotfix/*
- 禁止直接提交到 main 分支

---

## 環境變數

| 變數 | 說明 | 必要 |
|------|------|------|
| DB_CONNECTION_STRING | PostgreSQL 連線字串 | ✅ |
| JWT_SECRET_KEY | JWT 簽章金鑰 (≥32 字元) | ✅ |
| ADMIN_INITIAL_PASSWORD | 初始管理員密碼 | ⚠️ 未設定則隨機生成 |
| LLM_PROVIDER | AI 提供者 (openai / anthropic) | |
| LLM_API_KEY / OPENAI_API_KEY / ANTHROPIC_API_KEY | AI API 金鑰 | |
| LLM_BASE_URL | AI API Base URL | |
| LLM_DEFAULT_MODEL | 預設 AI 模型 | |
| LLM_MAX_TOKENS | 最大 Token 數 | |
| LLM_TEMPERATURE | 溫度參數 | |
| GRAFANA_ADMIN_PASSWORD | Grafana 管理員密碼 | monitoring |

---

## 啟動方式

### 完整啟動

`powershell
# 使用專案提供的啟動腳本
D:\ERP\start-all.ps1
`

### 個別啟動

`powershell
# 後端 API
cd D:\ERP\ErpBackend
dotnet run

# API Gateway
cd D:\ERP\ErpGateway
dotnet run

# 前端 Web
cd D:\ERP\erp-frontend
npm run dev          # http://localhost:5173

# 員工行動 App
cd D:\ERP\erp-employee-app
npm start

# 監控 (Prometheus + Grafana)
cd D:\ERP\monitoring
docker compose up -d
`

### 執行測試

`powershell
# 使用專案提供的測試腳本
D:\ERP\test-all.ps1

# 或手動執行
cd D:\ERP
dotnet test ERP.sln
cd erp-frontend && npm run lint && npm run build
cd erp-frontend && npx playwright test
`

### 初始化管理員

`powershell
D:\ERP\seed-admin.ps1
`

---

## 關鍵業務規則

### 庫存
- 庫存數量不得為負
- 庫存異動不可逆，錯誤操作須以反向異動平衡
- 銷售訂單 Confirm 後需保留庫存預留 (Inventory Reservation)

### 採購
- 收料須符合允收容差 (Delivery Tolerance) 設定
- 支援供應商評鑑與評分排程

### 生產
- 報工數量加總不可大於前一工序良品數
- 完工入庫數量不能大於工單實際產出良品數

### 財務
- 財務交易不可變（Immutable）
- 多幣別交易須同時記錄原幣與本位幣金額
- 實際成本結算須包含材料、人工、製造費用

### 研發
- ECN 生效後自動停用舊版 BOM
- MRP 與 MES 強制帶入最新版 BOM

### 品質
- 不良品處理支援：報廢、重工、退回供應商
- 退回供應商自動產生 Debit Note 扣減應付款

---

## AI 與智慧化功能

- **AI Planning Agent**：基於 LangGraph 的 Plan-and-Solve 策略，支援自主任務分解與執行監控
- **AI 排程 (AiScheduling)**：結合 LLM 的產能排程優化
- **AI 決策支援**：歷史大數據分析、訂單預測、採購時機建議
- **AI 財務稽核**：異常偵測、重複報支標記、價格異常警示
- **碳排可視化**：ESG 模組自動計算碳足跡並生成儀表板

---

## 解決方案專案 (Solution Projects)

| 專案 | 路徑 | 類型 |
|------|------|------|
| ErpBackend | ErpBackend\ErpBackend.csproj | ASP.NET Core 8.0 Web API |
| ErpGateway | ErpGateway\ErpGateway.csproj | ASP.NET Core 8.0 (YARP) |
| ErpBackend.Tests | ErpBackend.Tests\ErpBackend.Tests.csproj | xUnit 單元測試 |
| ErpBackend.IntegrationTests | ErpBackend.IntegrationTests\ErpBackend.IntegrationTests.csproj | 整合測試 |
| seed-admin | seed-admin\seed-admin.csproj | .NET 8 控制台工具 |
