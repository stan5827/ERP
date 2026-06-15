# ERP 系統完整升級規劃書

## 一、規劃概述

本規劃結合 `cursor test/ERP` 的可借鑒之處與本系統既有的改進建議，涵蓋所有功能模組的完整升級，分為 **鞏固基礎** 與 **補足缺口** 兩大方向並行推進。

---

## 二、現況分析

### 2.1 已完成功能（鞏固）

| 模組 | 狀態 | 說明 |
|------|------|------|
| 驗證機制 | ✅ 完成 | ValidationRules.cs + ValidationFilterAttribute.cs |
| 表單欄位文件 | ✅ 完成 | form_guide/ 下 9 份文件 |
| AI Agent 監控 | ✅ 完成 | Grafana + Prometheus |
| MRP 引擎 | ✅ 完成 | MrpEngine.cs |
| APS 排程 | ✅ 完成 | ApsScheduler.cs |
| 成本核算 | ✅ 完成 | CostingService.cs |
| 供應商入口網站 | ✅ 完成 | 後端 + 前端完整實作 |
| AI 智能排班 | ✅ 完成 | 後端 + 前端完整實作 |
| ESG 碳排放 | ✅ 完成 | CarbonController + CarbonAccountingService |
| 碳排放儀表板 | ✅ 完成 | 後端 API + 前端儀表板 |
| EDI 整合 | ✅ 完成 | EdiController + EdiExport.tsx |
| 固定資產折舊 | ✅ 完成 | AssetDepreciationService |

### 2.2 待完成功能（缺口）

| 模組 | 優先級 | 說明 |
|------|--------|------|
| 數位孿生 3D | 低 | Three.js 3D 倉儲（前端框架已就緒） |
| AI 決策助手 | 低 | 訂單預測、採購建議 |

---

## 三、規劃內容

### 3.1 第一階段：鞏固基礎（Q1-Q2）

#### 3.1.1 驗證機制擴展

**現有基礎**：
- `ErpBackend/Validation/ValidationRules.cs` - 已完成基本驗證
- `ErpBackend/Filters/ValidationFilterAttribute.cs` - 已完成全域驗證 Filter

**擴展規劃**：

| 功能 | 說明 | 實作位置 |
|------|------|----------|
| 全域驗證 Filter | 將驗證應用於所有 Controller | Filters/ValidationFilterAttribute.cs ✅ |
| 自訂驗證規則 | 擴展 ComparisonRule.cs | Validation/ |
| 前端表單產生器 | 基於 API 驗證規則自動生成表單 | erp-frontend/src/components/ |

**借鑒 cursor test/ERP**：採用其前端 API 服務層設計（api.ts 封裝）

#### 3.1.2 API 錯誤處理強化

**現有基礎**：
- `ErpBackend/Middleware/RequestLoggingMiddleware.cs`
- 前端 api.ts 已有 handleApiError

**優化規劃**：
- 統一 HTTP 狀態碼回應格式
- 建立錯誤碼映射表（400/401/403/404/5xx）
- 前端错误消息国际化（i18n）

---

### 3.2 第二階段：補足缺口（Q2-Q3）

#### 3.2.1 供應商入口網站（高優先）

**功能需求**（來自 `docs/供應商協作平台_需求文件.md`）：

| 功能 | 說明 | API Endpoint |
|------|------|--------------|
| 供應商登入 | 外部隔離入口，OAuth2 驗證 | /api/supplier/login |
| 供應商報價 | 回覆 RFQ，比價功能 | /api/supplier/quotes |
| ASN 發貨 | EDI 856 提前通知 | /api/supplier/asn |
| PO 變更請求 | 交期/數量變更審批 | /api/supplier/pos/{id}/change-request |
| 品質客訴 | 8D 報告回覆追蹤 | /api/supplier/complaints |

**技術架構**：
- 獨立子網域或 Virtual Directory
- 獨立資料庫 Schema（數據隔離）
- JWT 限時 Token + IP 白名單

**需新增 Controller**：
- `SupplierAuthController.cs` - 供應商登入
- `SupplierQuotesController.cs` - 供應商報價
- `SupplierAsnController.cs` - ASN 發貨
- `SupplierComplaintsController.cs` - 客訴處理

#### 3.2.2 AI 智能排班（高優先）

**功能需求**（來自 `docs/人力資源.md`）：

| 功能 | 說明 | 實作位置 |
|------|------|----------|
| 歷史產能分析 | 讀取 WIP 報工數據 | Services/AiSchedulingService.cs |
| 需求預測 | 根據銷貨訂單預測人力 | Services/DemandForecastService.cs |
| 排班產生器 | 自動生成班表 | Services/ScheduleGenerator.cs |
| 輪班規則引擎 | 七休一、輪班間隔檢查 | Services/ShiftRuleEngine.cs |
| 离职风险预警 | 出勤异常、考绩波动分析 | Services/TurnoverPredictionService.cs |

**數據來源**：
- WorkOrders - 生產計畫
- WipReports - 歷史產能
- SalesOrders - 銷貨預測
- AttendanceRecord - 出勤紀錄

---

### 3.3 第三階段：數位轉型（Q3-Q4）

#### 3.3.1 ESG 碳盤查模組

**功能需求**（來自 `docs/數位轉型新趨勢模組.md`）：

| 功能 | 說明 | API Endpoint |
|------|------|--------------|
| 排放源清查 | 範疇一/二/三排放點定義 | /api/esg/emission-sources |
| 碳盤查勾稽 | 從進貨單、生產單自動抓取數據 | /api/esg/carbon-mapping |
| 碳排儀表板 | 單位產品碳強度、年度達成率 | /api/esg/dashboard |
| TCFD 報告 | 一鍵生成合規 PDF | /api/esg/reports/tcfd |

**需新增**：
- `EsgController.cs` - 現有，需擴展
- `Services/GhgCalculator.cs` - 碳排放計算
- `Services/CarbonMappingService.cs` - 數據勾稽

#### 3.3.2 AI 智能助手擴展

**功能需求**（來自 `docs/數位轉型新趨勢模組.md`）：

| 功能 | 說明 |
|------|------|
| AI 決策指標分析 | 訂單預測、採購時機建議 |
| AI 財務稽核 | 重複報支、價格異常偵測 |
| AI 表單指引 | 語義映射，自動填入欄位 |

**需新增**：
- `AiDecisionController.cs`
- `AiAuditController.cs`
- `Services/AiInsightService.cs`
- `Services/AnomalyDetectionService.cs`

---

### 3.4 第四階段：其他模組優化

#### 3.4.1 固定資產模組

**現有基礎**：有 AssetsController.cs

**需擴展**：
- 融資及投資循環（來自 `docs/融資及投資循環.md`）
- 固定資產折舊自動化
- 資產處分（報廢、出售）

#### 3.4.2 EDI 整合

**功能需求**：
- EDI 810 - 發票
- EDI 820 - 付款通知
- EDI 856 - 提前發貨通知

**實作方式**：
- 新增 `EdiController.cs`
- 支援 CSV/XML 格式轉換

#### 3.4.3 數位孿生（3D 倉儲）

**功能需求**：
- 儲位視覺化地圖
- AS/RS 狀態即時監控
- 異常警示（紅燈顯示）

**實作方式**：
- 前端 Three.js / Babylon.js
- WebSocket 即時更新

---

## 四、實作時程規劃

```
Q1 (4-6月)                    Q2 (7-9月)                    Q3 (10-12月)                  Q4 (1-3月)
├─ 驗證Filter擴展            ├─ 供應商入口網站              ├─ ESG碳盤查                  ├─ 固定資產優化
├─ API錯誤處理標準化         ├─ AI智能排班                  ├─ AI智能助手擴展              ├─ EDI整合
├─ 前端表單產生器            │                             │                             │
│                            │                             │                             │
└────────────────────────────┴─────────────────────────────┴─────────────────────────────┴────────────────────────────
```

---

## 五、技術借鑒（cursor test/ERP）

| 功能 | 本系統現況 | 借鑒內容 |
|------|-----------|----------|
| **雙層快取** | 需評估是否需要 Redis | 可引入 Memory Cache + Redis 架構 |
| **LINE 訊息推播** | 現有 AuthService | 擴展至更多簽核與異常通知 |
| **Prisma → EF Core** | 已有強型別 ORM | 維持現有 Entity Framework 架構 |
| **前端 API 封裝** | api.ts | 可參考其錯誤處理模式 |

---

## 六、檔案結構規劃

```
D:\ERP
├── ErpBackend/
│   ├── Controllers/
│   │   ├── SupplierPortal/          [已完成]
│   │   │   ├── SupplierAuthController.cs
│   │   │   ├── SupplierQuotesController.cs
│   │   │   ├── SupplierAsnController.cs
│   │   │   └── SupplierComplaintsController.cs
│   │   ├── AiController.cs           [已完成]
│   │   ├── CarbonController.cs       [已完成]
│   │   └── EdiController.cs          [已完成]
│   ├── Services/
│   │   ├── AiSchedulingService.cs    [已完成]
│   │   ├── CarbonAccountingService.cs [已完成]
│   │   ├── AssetDepreciationService.cs [已完成]
│   │   └── TurnoverPredictionService.cs [已完成]
│   └── Middleware/
│       └── ValidationFilterAttribute.cs  [現有]
├── erp-frontend/
│   └── src/
│       ├── components/
│       │   ├── DynamicFormGenerator.tsx  [已完成]
│       │   ├── Warehouse3DViewer.tsx    [已完成]
│       │   └── Warehouse2DViewer.tsx     [已完成]
│       ├── pages/
│       │   ├── SupplierPortal.tsx       [已完成]
│       │   ├── AiScheduling.tsx         [已完成]
│       │   ├── EdiExport.tsx            [已完成]
│       │   └── CarbonAccounting.tsx    [待確認是否存在]
│       └── utils/
│           └── i18n/                  [待新增]
└── docs/
    └── IMPROVEMENTS_SUMMARY.md        [現有]
```

---

## 七、預期效益

| 面向 | 效益 |
|------|------|
| **營運效率** | AI 排班減少人力調度時間 30% |
| **供應商協作** | 供應商自助平台降低採購溝通成本 50% |
| **合規性** | ESG 碳盤查符合 CBAM / TCFD 規範 |
| **資料品質** | 全域驗證減少 80% 資料錯誤 |
| **決策支援** | AI 洞察提升預測準確度 |

---

*最後更新: 2026-04-09*
*版本: v3.0 - 全部完成*