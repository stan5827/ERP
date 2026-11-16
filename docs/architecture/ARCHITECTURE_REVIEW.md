# ERP Backend 架構診斷報告

## 執行摘要

| 指標 | 數值 | 狀態 |
|------|------|------|
| 總代碼行數 (LOC) | 74,591 | 過大 |
| 類/介面/記錄總數 | 719 | 單一巨獸 |
| 控制器數量 | 76 | 過多 |
| 服務數量 | 38 | 過多 |
| 模型數量 | ~240 | 單一巨獸 |
| DTO 數量 | ~90 | 過多 |
| 編譯錯誤 | 0 | 通過 |
| 測試通過 | 61 | 覆蓋率低 |


## 五、Code Quality Metrics

### 5.1 代碼度量

| 指標 | 當前值 | 推薦值 | 狀態 |
|------|--------|--------|------|
| 平均方法長度 | ~15 行 | < 20 行 | 通過 |
| 平均類長度 | ~120 行 | < 500 行 | 部分超标 |
| 最大單檔 (AIPlanningService) | 923 行 | < 500 行 | 超标 |
| 測試覆蓋率 | ~1% (61 tests / 74591 LOC) | > 80% | 嚴重不足 |
| 圈複雜度 | 未知 | < 10 | 需檢查 |

### 5.2 代碼味道 (Code Smells)

| 味道 | 數量 | 嚴重度 |
|------|------|--------|
| 上帝類 (Large Class) | 3 | 高 |
| 功能逸移 (Feature Envy) | 17 | 中 |
| 重複代碼 (Duplication) | 6 個本地 DTO | 中 |
| 長參數列表 | 多個 | 低 |
| 魔法字符串 | 16 個控制器 | 中 |

## 六、Security Review

| 問題 | 嚴重度 | 位置 |
|------|--------|------|
| JWT 配置硬編碼 | 中 | Program.cs |
| 密碼強度檢查 | 良好 | UsersController |
| SQL 注入防護 | 良好 | 使用 EF Core |
| XSS 防護 | 需檢查 | 輸出編碼 |
| CORS 配置 | 需檢查 | Program.cs |

## 七、Performance Review

| 問題 | 影響 | 位置 |
|------|------|------|
| N+1 查詢 | 高 | 多個控制器 |
| 缺少緩存策略 | 中 | 查詢密集型接口 |
| 不分頁大結果集 | 中 | Dashboard, Reports |
| 同步阻塞調用 | 高 | ApsIntegrationController, IqcReportsController |

## 八、Refactor Roadmap

### Phase 1: 緊急修復 (P0 - 1週)

| # | 項目 | 工時 | 風險 |
|---|------|------|------|
| 1 | 修復阻塞調用 (.Result/.Wait) | 0.5天 | 低 |
| 2 | 建立缺失的 Solution 檔案 | 0.5天 | 低 |
| 3 | 填補空控制器 (LeaveRequests) | 0.5天 | 低 |

### Phase 2: 架構重構 (P1 - 2週)

| # | 項目 | 工時 | 風險 |
|---|------|------|------|
| 4 | 建立 Repository 抽象層 | 2天 | 中 |
| 5 | 移除 43 個控制器的 DbContext 直接依賴 | 3天 | 中 |
| 6 | 將本地 DTO 移至 DTOs/ 目錄 | 1天 | 低 |
| 7 | 實現缺失的服務接口 | 2天 | 低 |

### Phase 3: 代碼拆分 (P2 - 3週)

| # | 項目 | 工時 | 風險 |
|---|------|------|------|
| 8 | 拆分 AIPlanningService (923行->多個小服務) | 3天 | 高 |
| 9 | 拆分 MrpEngine (771行->多個小服務) | 3天 | 高 |
| 10 | 拆分 ToolFactory (920行->多個小服務) | 3天 | 高 |
| 11 | 拆分大型模型文件 | 3天 | 中 |

### Phase 4: 品質提升 (P3 - 持續)

| # | 項目 | 工時 | 風險 |
|---|------|------|------|
| 12 | 建立自動化測試 (目標 80% 覆蓋) | 10天 | 中 |
| 13 | 實施 Static Analysis (SonarQube) | 2天 | 低 |
| 14 | 導入集成測試框架 | 3天 | 中 |

## 九、總結

### 關鍵發現

1. **單一巨獸專案**: 74,591 行代碼，76 個控制器，38 個服務，全部在單一專案中
2. **架構違規嚴重**: 56% 控制器直接依賴 DbContext，違反依賴倒轉原則
3. **測試覆蓋率極低**: 僅 61 個測試對應 74,591 行代碼 (~1%)
4. **大型服務未拆分**: 3 個服務超過 900 行，難以維護
5. **缺少 Solution 檔案**: 無法整體構建和管理專案依賴

### 優先修復順序

\\\n1. 緊急: 修復阻塞調用 (ApsIntegrationController, IqcReportsController)
2. 緊急: 建立 Solution 檔案
3. 高優先級: 拆分大型服務 (AIPlanningService, MrpEngine, ToolFactory)
4. 高優先級: 移除 DbContext 直接依賴
5. 中優先級: 補充缺失測試
6. 低優先級: 實施 Static Analysis
\\\n
## 附錄：詳細數據

### A. 控制器分析

| 類別 | 計數 | 百分比 |
|------|------|--------|
| 直接使用 DbContext | 43 | 56% |
| 使用服務層 | 18 | 24% |
| 同時使用兩者 | 6 | 8% |
| 空控制器 | 1 | 1% |
| 無依賴 | 8 | 10% |

### B. 服務分析

| 類別 | 計數 |
|------|------|
| 已實現接口 | 29 |
| 未實現接口 | 8 |
| 大型服務 (>500行) | 6 |
| 跨服務依賴 (>3個) | 15 |

### C. 模型分析

| 文件 | 類數 | 行數 | 導航屬性數 |
|------|------|------|------------|
| InventoryModels.cs | 24 | 489 | 337 |
| HrModels.cs | 19 | 443 | 331 |
| ProcurementModels.cs | 19 | 390 | 276 |
| SalesModels.cs | 15 | 357 | 255 |
| MesModels.cs | 16 | 274 | 181 |


## 三、Solution Dependency Analysis

### 3.1 專案依賴圖

\\\n+-------------------------------------------------------------+
|                     ErpBackend.sln                         |
|                        (缺失)                               |
+-------------------------------------------------------------+
                              |
              +---------------+---------------+
              v               v               v
    +--------------+  +--------------+  +-----------------+
    | ErpBackend   |  |   Tests      |  | IntegrationTests |
    |  (Web API)   |-->|  (xUnit)     |-->|    (xUnit)      |
    +--------------+  +--------------+  +-----------------+
         |
         v
    +--------------+
    |  PostgreSQL  |
    |  (DbContext) |
    +--------------+
\\\n
### 3.2 NuGet 依賴分析

| 套件 | 版本 | 狀態 | 建議 |
|------|------|------|------|
| Microsoft.EntityFrameworkCore | 8.0.11 | 最新 | - |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | 一致 | - |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | 一致 | - |
| BCrypt.Net-Next | 4.0.3 | 舊版 | 考慮升級 |
| Swashbuckle.AspNetCore | 6.9.0 | 舊版 | 升級至 7.x |
| prometheus-net.AspNetCore | 8.2.1 | 一致 | - |
| DotNetEnv | 2.5.0 | 穩定 | - |

## 四、Layer Analysis

### 4.1 當前架構 (問題)

\\\n+-------------------------------------------------------------+
|  Presentation Layer (Controllers)                          |
|  直接依賴 DbContext (43個控制器)                            |
|  包含業務邏輯 (非純展示層)                                  |
+-------------------------------------------------------------+
                            |
                            v 違規
+-------------------------------------------------------------+
|  Data Access Layer (DbContext)                             |
|  被多層同時依賴                                            |
|  缺乏Repository模式                                        |
+-------------------------------------------------------------+
\\\n
### 4.2 期望架構 (Clean Architecture)

\\\n+-------------------------------------------------------------+
|  Presentation Layer (Controllers)                          |
|  -> 僅負責 HTTP 請求/回應                                  |
|  -> 依賴 Interfaces                                        |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Application Layer (Services)                              |
|  -> 業務邏輯核心                                            |
|  -> 依賴 Domain Entities                                    |
|  -> 實現接口隔離                                            |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Domain Layer (Entities)                                   |
|  -> 領域模型                                                |
|  -> 純業務規則                                              |
|  -> 無外部依賴                                              |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Infrastructure Layer                                      |
|  -> DbContext / Repository                                  |
|  -> 外部服務集成                                            |
|  -> 依賴倒轉實現                                            |
+-------------------------------------------------------------+
\\\n

### 依賴關係矩陣

| 層級 | 依賴方向 | 違反 Clean Architecture |
|------|----------|------------------------|
| Controllers → Services | 正確 | - |
| Services → Models | 正確 | - |
| Controllers → Models | 嚴重違規 | 43個控制器 |
| Services → Services | 部分違規 | 32個服務 |
| Models → DTOs | 違規 | 跨層引用 |

## 二、Architecture Violations

### 2.1 Controller 層違規 (嚴重度: 高)

| 問題 | 影響範圍 | 計數 |
|------|----------|------|
| 直接使用 _db (DbContext) | 43 個控制器 | 56% |
| 未使用服務層 | 43 個控制器 | 56% |
| 本地 DTO 定義 | 6 個控制器 | FinanceController, DepartmentsController, JobGradesController, JobTitlesController, MpsController, SalesReportsController |
| 硬編碼狀態字符串 | 16 個控制器 | 如 pending, confirmed |
| 阻塞調用 (.Result/.Wait) | 2 個控制器 | ApsIntegrationController, IqcReportsController |
| 缺少 Exception Handling | 17 個控制器 | 超過 5 個 action |

### 2.2 服務層問題 (嚴重度: 中)

| 問題 | 影響範圍 |
|------|----------|
| 未實現接口 | UomConversionService, ApprovalWorkflowService, AgentMetricsService, CalendarReminderService, StateMachineService, ExchangeRateSyncService, SupplierRatingSchedulerService, AssetDepreciationSchedulerService |
| 大型服務 (超過 500 行) | AIPlanningService (923行), MrpEngine (771行), ToolFactory (920行), KpiService (572行), MpsService (492行), ApsScheduler (485行) |
| 跨服務依賴 | AIPlanningService 引用 13 個其他服務 |

### 2.3 模型層問題 (嚴重度: 中)

| 問題 | 影響範圍 |
|------|----------|
| 大型模型文件 | InventoryModels.cs (24類/489行), HrModels.cs (19類/443行), ProcurementModels.cs (19類/390行) |
| 導航屬性過多 | InventoryModels.cs (337個), HrModels.cs (331個) |
| 缺乏 Domain Events | 全部模型 |
