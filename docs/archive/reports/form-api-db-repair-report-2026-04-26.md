# ERP 專案表單 / 流程 / API / 資料庫 修護分析報告

日期: 2026-04-26
工作區: `D:\ERP`

## 1. 本次分析範圍

本次針對以下區塊進行靜態分析、編譯驗證與局部修整：

- 前端表單層：`erp-frontend/src/components/FormModal.tsx`、`erp-frontend/src/hooks/useFormValidation.ts`
- 前端 API 層：`erp-frontend/src/api/api.ts`、`erp-frontend/src/hooks/useApi.ts`
- 前端頁面流程：`Inventory.tsx`、`BudgetAnalysis.tsx`、`MrpPlanning.tsx`、`Collaboration.tsx`、`Benchmark.tsx`
- 後端 API / Controller：`ErpBackend/Controllers/*`
- 資料庫存取層：`ErpBackend/Data/ErpGeneralDbContext.cs`
- 啟動 / migration / seed 流程：`ErpBackend/Program.cs`

## 2. 本次已完成修復

### 已修復的阻斷問題

1. 後端 `SkusController` 建立 SKU 時存在重複欄位指定，造成 `dotnet build` 失敗。
2. 前端 `SkuDto` 介面重複宣告多組 UOM 欄位，造成 TypeScript 編譯失敗。
3. `budgetApi` 缺少 `alerts`、`anomalies`、`analysis` 方法，導致預算分析頁 hook 與 API 契約斷裂。
4. `BudgetAnalysis` hook 沒有解開 `ApiResponse<T>`，頁面直接把回傳值當陣列/物件使用，造成型別錯誤。
5. `MrpPlanning` 呼叫不存在的 `mrpApi.getPlannedOrders`，且 `FormModal` 傳入不存在的 `size` 屬性。
6. `Benchmark.tsx` 使用錯誤的元件相對路徑，且該 import 未實際使用。
7. `Collaboration.tsx` 依賴不存在的 `useSupplierAsns` hook，編譯中斷。
8. `Inventory.tsx` 在倉庫類型維護 modal 中誤呼叫外層 SKU 表單狀態 setter，導致作用域錯誤。
9. `ESG` 頁面使用 `emissionFactor`，但 `CarbonEmissionRecordDto` 缺少該欄位定義。

### 修復後驗證結果

- `dotnet build ErpBackend/ErpBackend.csproj`: 成功
- `npm run build` in `erp-frontend`: 成功
- `dotnet test ErpBackend.Tests/ErpBackend.Tests.csproj`: 53/53 通過
- `dotnet test ErpBackend.IntegrationTests/ErpBackend.IntegrationTests.csproj`: 8/8 通過

## 3. 目前確認的結構性風險

### 高優先

1. `SupplierAsnController` 目前僅有 `ping`，代表 ASN 流程尚未真正落地。
   - 影響：供應商協同、到貨預報、入庫前追蹤流程無法形成完整閉環。
   - 目前處理：前端先降級為空資料顯示，避免編譯失敗。

2. 前端 `api.ts` 單檔過大，兼任 DTO、axios 攔截器、所有模組 API 定義。
   - 影響：欄位重複、契約漂移、改一處壞多處的風險高。
   - 直接證據：本次 `SkuDto` 與 `budgetApi` 問題都出自同一檔。

3. 全域 request interceptor 會自動做 `sanitize` 與 `PascalCase` 轉換。
   - 影響：若個別 controller 預期 camelCase、flat payload、或部分欄位需保留空字串，容易產生隱性表單送出錯誤。
   - 目前狀態：已有 skip path，但規則仍偏脆弱。

### 中優先

4. `Program.cs` 在啟動時執行 `Database.Migrate()` 與大量 seed。
   - 影響：部署啟動時間、seed 邏輯耦合、正式環境啟動風險偏高。
   - 建議：將 seed 與 migration 控制分離，至少用環境變數切分。

5. 前端多數頁面直接在頁面中拼組大量表單欄位與業務規則。
   - 影響：欄位必填、預設值、 API payload 轉換規則容易重複且不一致。

6. API base URL 目前硬編碼為 `http://localhost:5100/api/erp`。
   - 影響：環境切換、測試環境、正式環境部署彈性不足。

### 低優先

7. `MrpEngine.cs` 仍有 `async` 無 `await` 警告。
   - 影響：功能不一定錯，但可讀性與意圖不清，未來容易留下假非同步邏輯。

## 4. 表單 / 流程 / API / DB 的現況判讀

### 表單層

- 已具備通用表單元件 `FormModal` 與欄位驗證工具，基礎可重用性不差。
- 但不同頁面仍各自拼欄位、各自處理 payload，缺乏模組級 schema。
- 真正的風險不是 UI，而是欄位命名與 API DTO 同步沒有單一來源。

### 流程層

- 採購、庫存、MRP、ESG、預算等流程都已有 UI 與部份 API。
- 但部分流程處於「頁面已存在，後端尚未完整」狀態，ASN 是最明顯案例。
- 這代表系統目前有展示面完整、交易閉環未完整的模組。

### API 層

- Controller 數量多、覆蓋模組廣，但缺少一致的契約治理。
- 前端 hook 有的回 `ApiResponse<T>`，有的回 `T`，使用方式不統一，容易再出現本次預算分析這種問題。

### 資料庫層

- `ErpGeneralDbContext` 覆蓋模組非常完整，資料模型集中。
- 優點是整體一致；缺點是上下文過大、migration 與模組邊界管理成本高。
- 目前從建置與測試看來，資料存取主幹可運作，但仍建議分階段做 bounded context 化整理。

## 5. 建議修護路線

### Phase 1: 穩定化

- 將前端 API 按模組拆檔：`inventoryApi.ts`、`budgetApi.ts`、`esgApi.ts`
- 規範所有 hook 一律回傳解包後資料，不直接把 `ApiResponse<T>` 暴露給頁面
- 補齊 `.env` 或 `VITE_API_BASE_URL`，移除 API base URL 硬編碼
- 針對 `SupplierAsn` 定義最小可用 DTO、列表 API、建立 API、狀態流轉 API

### Phase 2: 契約治理

- 前後端共用 DTO 命名規格
- 針對高頻表單建立 schema 層
- 逐步減少全域 interceptor 的隱式轉換，改為明確 serializer

### Phase 3: 資料庫與部署治理

- 分離 migration、seed、startup 初始化責任
- 將測試資料 seed 與正式 seed 分開
- 盤點 `DbContext` 是否需要模組化或至少拆 configuration 檔

## 6. 本次修護結論

目前專案已從「前後端都存在編譯阻斷」恢復到：

- 後端可編譯
- 前端可編譯與打包
- 單元測試與整合測試皆通過

但這不等於所有流程都已完整。現階段最重要的實際缺口是：

1. ASN 流程尚未完成
2. API 契約治理不足
3. 表單 payload 轉換過度依賴全域攔截器
4. 啟動時 migration/seed 耦合偏高

本次建議把下一步優先排在 `SupplierAsn 流程補完` 與 `前端 API 分模組拆分`，這兩項會直接降低後續表單與流程故障率。
