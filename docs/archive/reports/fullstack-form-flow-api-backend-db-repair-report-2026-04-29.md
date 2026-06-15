# ERP 前後端 / 表單 / 流程 / API / 資料庫 修整優化與規劃修護報告

日期：2026-04-29

## 1. 本次檢測範圍

本次檢測涵蓋以下面向：

- 前端表單與頁面流程：`erp-frontend/src/pages`
- 前端 API / hook / 型別層：`erp-frontend/src/api`、`erp-frontend/src/hooks`
- 後端控制器與服務：`ErpBackend/Controllers`、`ErpBackend/Services`
- 後端資料模型與資料庫存取：`ErpBackend/Data`、`ErpBackend/Models`、`ErpBackend/DTOs`
- 專案建置與測試健康度

## 2. 執行摘要

目前專案不是「不能動」，而是處於「功能已鋪開，但契約一致性與模組邊界開始明顯鬆動」的狀態。

本次檢測最重要的結論有四個：

1. 前端目前存在 compile-break，不能視為穩定交付狀態。
2. 前後端 API 契約與前端 hook / page 使用方式已經發生多處漂移。
3. 後端可以建置、整合測試可通過，但單元測試基礎已被 `DbContext` 建構方式破壞。
4. 資料模型、DTO、API、頁面表單仍有大型集中檔，後續每次擴充都會提高回歸風險。

## 3. 本次驗證結果

### 3.1 建置結果

- 前端 `npm run build`：失敗
- 後端 `dotnet build ErpBackend/ErpBackend.csproj -o D:/ERP/scratch/backend-analysis-20260429`：成功

### 3.2 測試結果

- 單元測試 `ErpBackend.Tests`：`39` 通過，`14` 失敗
- 整合測試 `ErpBackend.IntegrationTests`：`8/8` 通過

### 3.3 驗證判讀

這代表目前後端執行主體仍可工作，但前端交付面與單元測試防線都不穩：

- 前端無法完整 build，表示型別契約已經不一致。
- 單元測試失敗集中在 `ErpGeneralDbContext` 代理建立，表示可測試性退化。
- 整合測試仍通過，說明核心 HTTP / DB 路徑尚未全面崩壞，但這不代表前端可正常使用。

## 4. 關鍵發現

### P0：前端目前 compile-broken，屬於立即修復項

以下問題會直接阻止前端產出：

- [procurementApi.ts](/D:/ERP/erp-frontend/src/api/procurementApi.ts) 引用了 `GoodsReceiptDto`、`SupplierAsnDto`，但 [api.ts](/D:/ERP/erp-frontend/src/api/api.ts) 目前未正確對外提供這些型別。
- [useApi.ts](/D:/ERP/erp-frontend/src/hooks/useApi.ts) 仍使用 `notificationsApi`、`budgetApi.getAlerts()`、`budgetApi.getAnomalies()`、`budgetApi.analyze()`、`esgApi.getRecentEmissions()` 等不存在或已改名方法。
- `MRP / APS / SystemMonitoring / Inventory` 等頁面仍依賴已變動的 DTO 欄位或 API 方法。

這不是單一檔案 typo，而是「前端 API 契約治理」問題。

### P0：單元測試基礎已失效，代表可回歸性下降

[ErpGeneralDbContext.cs](/D:/ERP/ErpBackend/Data/ErpGeneralDbContext.cs:27) 目前建構子為：

- `ErpGeneralDbContext(DbContextOptions<ErpGeneralDbContext> options, ITenantProvider tenantProvider = null!)`

單元測試使用 Moq 建立 `ErpGeneralDbContext` proxy 時無法匹配此建構方式，導致：

- `AIPlanningServiceTests`
- `ToolFactoryTests`

共 `14` 筆測試失敗。

這表示目前後端不只是測試數量偏少，而是測試基礎設施本身已不健康。

### P1：前端 API / hook / 頁面三層契約長期漂移

目前前端有幾個集中風險點：

- [api.ts](/D:/ERP/erp-frontend/src/api/api.ts) 約 `2353` 行
- [useApi.ts](/D:/ERP/erp-frontend/src/hooks/useApi.ts) 約 `632` 行
- [Inventory.tsx](/D:/ERP/erp-frontend/src/pages/Inventory.tsx) 約 `1495` 行
- [Procurement.tsx](/D:/ERP/erp-frontend/src/pages/Procurement.tsx) 約 `1111` 行

這代表：

- 型別、axios 呼叫、資料整形、頁面互動耦合過高
- 任一模組增修都容易影響其他頁面
- 表單欄位與 DTO 名稱難以穩定同步

### P1：表單流程仍混有即席互動與非標準資料流

目前仍可看到直接使用：

- `prompt(...)`：例如 [Procurement.tsx](/D:/ERP/erp-frontend/src/pages/Procurement.tsx:229)、[Collaboration.tsx](/D:/ERP/erp-frontend/src/pages/Collaboration.tsx:38)
- `localStorage`：例如 [SupplierPortal.tsx](/D:/ERP/erp-frontend/src/pages/SupplierPortal.tsx:56)、[SupplierRfqList.tsx](/D:/ERP/erp-frontend/src/pages/SupplierRfqList.tsx:36)
- 直接 `axios` 呼叫：例如 [CostAnalysisDashboard.tsx](/D:/ERP/erp-frontend/src/pages/CostAnalysisDashboard.tsx:25)

這些做法短期可用，但長期會造成：

- 流程權限與 token 管理分散
- 表單提交流程不一致
- 錯誤處理與 loading 狀態難以統一

### P1：後端模型與 DTO 仍為大型集中檔

目前後端主要集中度如下：

- [ErpGeneralDbContext.cs](/D:/ERP/ErpBackend/Data/ErpGeneralDbContext.cs) 約 `743` 行
- [DTOs.cs](/D:/ERP/ErpBackend/DTOs/DTOs.cs) 約 `786` 行
- [ProcurementModels.cs](/D:/ERP/ErpBackend/Models/Procurement/ProcurementModels.cs) 為採購主模型集中檔
- 全部 controller 共 `87` 支

這代表模組雖多，但實際邊界還不夠乾淨：

- DTO 與 domain 邊界容易混淆
- model / controller / service 命名與責任未完全模組化
- 新需求加入時，集中檔會持續膨脹

### P2：流程已逐步形成，但仍存在雙軌與不完整鏈路

正向進展：

- `Supplier ASN` 已有正式 controller
- `GoodsReceiptsController` 已補上，`ASN -> GR` 流程已可成立
- `Procurement` 已開始接入 `GR` 視圖

仍需補強的地方：

- `Procure-to-Pay` 的前端追蹤面還未完整串到 `AP`
- `Supplier Portal / RFQ / PO / ASN / GR` 仍有部分頁面走舊 API 寫法
- `表單 schema`、`欄位規則`、`狀態機` 尚未真正集中治理

## 5. 模組別診斷

### 5.1 前端表單

優點：

- 已建立共用 [FormModal.tsx](/D:/ERP/erp-frontend/src/components/FormModal.tsx)
- 多數模組已朝表單元件化前進

問題：

- `FormModal` 偏向萬用承載器，不等同於欄位 schema 管理
- 表單欄位定義、預設值、驗證規則仍大量散落在各 page
- 部分大型頁面同時承擔列表、查詢、表單、審批、明細編輯、列印等責任

結論：

目前是「有共用元件，但沒有共用表單治理層」。

### 5.2 流程與頁面導航

優點：

- 採購、庫存、HR、製造、品質、ESG、財務等主模組皆已成形
- 部分跨流程路徑已打通，如 `PO -> GR`、`ASN -> GR`

問題：

- 流程狀態切換仍散在頁面事件中
- 使用 `prompt` 等即席互動，代表流程節點尚未產品化
- 頁面層與流程層尚未分離，容易出現「UI 按鈕直接承擔業務規則」

### 5.3 API 與前後端契約

優點：

- 已有模組化方向，例如 `procurementApi.ts`
- 後端 controller 命名大致與業務模組對應

問題：

- 前端 monolithic `api.ts` 仍然是主入口
- `hooks` 對 API 方法名稱依賴過深，缺乏契約測試
- DTO / hook / page / controller 的命名一致性不足

### 5.4 後端程式

優點：

- 後端主程式可建置
- controller 覆蓋範圍完整
- 整合測試仍可反映部分主流程可用

問題：

- `DbContext` 與 DTO 集中度偏高
- 單元測試替身建立失敗，反映建構模式與測試模式不相容
- 缺乏更清楚的 application service / domain service 邊界

### 5.5 資料庫與資料模型

優點：

- 模型面已覆蓋 ERP 主題域
- `Asn / GoodsReceipt / AccountsPayable` 等核心資料已具備關聯基礎

問題：

- entity configuration 仍應進一步分檔與模組化
- 主資料、交易資料、流程事件、分析資料尚未清楚分層
- 目前看起來仍偏 transactional schema，分析與 BI 資料層不足

## 6. 修護優先順序

### 第一優先級：立即修復

1. 修正前端 compile errors
2. 對齊 `api.ts / procurementApi.ts / useApi.ts / page` 契約
3. 恢復 `notifications / budget / esg / mrp / inventory / aps` 的缺漏方法或調整呼叫端
4. 修正 `ErpGeneralDbContext` 測試替身問題，恢復單元測試

### 第二優先級：結構整理

1. 持續將 `Procurement / Supplier Portal / AP / RFQ` 從 `api.ts` 抽離
2. 建立表單 schema / validator / defaulting 中央層
3. 將大型 page 拆為 `list + detail + form + actions + hooks`
4. 將 `DTOs.cs`、`DbContext`、大型 models 分模組整理

### 第三優先級：流程治理

1. 為 `PO / PR / ASN / GR / AP / SO / WO` 建立狀態機
2. 建立共用流程事件紀錄與審計欄位
3. 消除 `prompt / localStorage scattered usage / direct axios` 的流程分叉

### 第四優先級：資料與分析層

1. 建立主資料治理規範
2. 建立分析查詢層與 KPI 資料集
3. 為 BI / 預警 / 預測能力做資料結構準備

## 7. 建議執行計畫

### Phase A：7 天止血

- 修正前端所有 compile errors
- 修復 `useApi.ts` 與 `api.ts` 的失配
- 讓單元測試至少回到可跑基線
- 重新建立「目前可交付版本」的 build green baseline

### Phase B：30 天結構整頓

- 拆 `api.ts` 高風險模組
- 拆 `Inventory.tsx`、`Procurement.tsx`
- 將 `FormModal` 延伸為 schema-driven 表單層
- 為 `Supplier / Procurement / Inventory` 建立穩定 hook 與型別層

### Phase C：60 到 90 天流程重構

- 建立完整 Procure-to-Pay、Order-to-Cash、Plan-to-Produce 狀態模型
- 補流程追蹤與審計
- 建立分析層與管理指標資料集

## 8. 建議產出物

建議下一輪修護不只做 code fix，也同步產出以下文件：

- 模組 API 契約表
- 表單欄位 schema 標準
- 主資料字典
- 流程狀態機定義
- 測試修復清單
- 前端 compile 修復清單

## 9. 本次結論

這個專案的基礎並不差，真正的問題不是「沒有功能」，而是：

- 功能鋪得比治理快
- 模組長得比契約穩定度快
- 頁面發展得比表單 / API / 測試邊界成熟快

因此目前最正確的修護方向不是再加新頁，而是先把：

- 前端 build
- API 契約
- 單元測試基線
- 表單治理層
- 模組 API 邊界

這五件事穩住。

一旦這五件事穩住，後面的流程擴充、BI、預測、外部整合才會開始變便宜。
