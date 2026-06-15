# AI 規劃代理系統 (AI Planning Agent) 操作手冊

## 一、系統概述

AI 規劃代理系統是一個 ERP AI 代理監控系統，支援 Planning 模式自主任務分解，實現 Plan-and-Solve 策略。目標為準確率 > 95%，ROI > 200%。

---

## 二、系統架構

### 2.1 三節點架構 (Planner → Executor → Reviewer)

```
┌─────────────────────────────────────────────────────────────┐
│                      Client Layer                            │
│                  (React Frontend + API Gateway)             │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                  AI Planning Agent                          │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │   Planner   │───▶│  Executor   │───▶│   Reviewer  │     │
│  │     節點     │    │     節點    │    │     節點     │     │
│  └─────────────┘    └─────────────┘    └─────────────┘     │
│       │                   │                   │              │
└───────┼───────────────────┼───────────────────┼──────────────┘
        │                   │                   │
┌───────▼───────────────────▼───────────────────▼──────────────┐
│                      Tool Layer                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  搜尋引擎    │  │   計算模組   │  │    ERP API 工具     │  │
│  │  工具        │  │   工具       │  │                     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 核心元件說明

| 元件 | 職責 | 技術 |
|------|------|------|
| **Planner Node** | 任務分解、策略選擇、優化迭代 | 自定義 C# 邏輯 |
| **Executor Node** | 執行工具、結果驗證、錯誤恢復 | 自定義 C# 邏輯 |
| **Review Node** | 成果審核、品質檢查、決策確認 | 自定義 C# 邏輯 |
| **Tool Integrations** | 搜尋、計算、ERP API 整合 | ToolFactory + ErpIntegrationService |

---

## 三、核心服務 (Core Services)

### 3.1 AIPlanningService (`Services/AIPlanningService.cs`)

**主要功能：**
- `CreatePlanAsync(PlanningRequest)` - 建立執行計畫
- `ExecutePlanAsync(ExecutionRequest)` - 執行計畫
- `ReviewResultAsync(ReviewRequest)` - 審核結果
- `ValidateKpiAsync()` - KPI 驗證
- `GetAgentStateAsync(executionId)` - 取得代理狀態
- `GetExecutionHistoryAsync(limit)` - 取得執行歷史

**主要類別：**
```csharp
// 任務請求
public class PlanningRequest {
    string TaskDescription { get; set; }    // 任務描述
    string? Context { get; set; }          // 上下文
    Dictionary<string, object>? Parameters { get; set; }
    string? ModelId { get; set; }
}

// 規劃回應
public class PlanningResponse {
    bool Success { get; set; }
    string? PlanId { get; set; }
    List<PlanTask> Tasks { get; set; }      // 子任務清單
    string Strategy { get; set; }           // serial/parallel/mixed
    decimal Confidence { get; set; }        // 信心度
    List<string>? Reasoning { get; set; }    // 推理過程
}

// 執行回應
public class ExecutionResponse {
    bool Success { get; set; }
    string? ExecutionId { get; set; }
    List<TaskResult> Results { get; set; }
    decimal Duration { get; set; }          // 執行時間(秒)
}

// 審核回應
public class ReviewResponse {
    bool Approved { get; set; }
    decimal QualityScore { get; set; }      // 品質分數
    List<string> Issues { get; set; }        // 發現的問題
    string? Recommendations { get; set; }   // 建議
    List<string>? SuggestedFixes { get; set; }
}
```

### 3.2 ToolFactory (`Services/ToolFactory.cs`)

**功能：** 提供 40+ ERP 工具的統一存取介面

**主要方法：**
```csharp
Task<ToolResult> ExecuteToolAsync(string toolId, Dictionary<string, object> parameters)
List<ErpToolDefinition> GetAvailableTools()
```

### 3.3 LlmService (`Services/LlmService.cs`)

**功能：** 整合 OpenAI GPT-4 和 Claude AI 模型

**支援的模型：**
- `OpenAiLlmService` - OpenAI GPT-4
- `ClaudeLlmService` - Anthropic Claude

**配置選項：**
```csharp
public class LlmConfig {
    string Provider { get; set; }           // "openai" 或 "anthropic"
    string ApiKey { get; set; }
    string BaseUrl { get; set; }           // API 端點
    string DefaultModel { get; set; }       // 預設模型
    int MaxTokens { get; set; } = 2000      // 最大 tokens
    double Temperature { get; set; } = 0.7  // 溫度參數
    int TimeoutSeconds { get; set; } = 60   // 逾時秒數
}
```

### 3.4 ErpIntegrationService (`Services/ErpIntegrationService.cs`)

**功能：** ERP 後端資料庫操作整合

**主要方法：**
- `CreateWorkOrderAsync(skuId, quantity, priority)` - 建立工單
- `UpdateWorkOrderStatusAsync(woId, status)` - 更新工單狀態
- `CompleteWorkOrderAsync(woId, quantity)` - 完工工單
- `CreatePurchaseRequisitionAsync(supplierId, items)` - 建立採購申請
- `AdjustInventoryAsync(skuId, warehouseId, quantity, reason)` - 庫存調整
- `SearchInventoryAsync(skuCode, warehouseId)` - 庫存搜尋
- `SearchWorkOrdersAsync(woNumber, status)` - 工單搜尋
- `SearchPurchaseOrdersAsync(poNumber, status)` - 採購訂單搜尋
- `GenerateInventoryReportAsync(warehouseId, dateFrom, dateTo)` - 庫存報告

### 3.5 AgentMetricsService (`Services/AgentMetricsService.cs`)

**功能：** 收集與記錄 AI Agent 指標 (使用 Prometheus)

**指標項目：**
- `erp_ai_agent_latency_seconds` - API 延遲 (Histogram)
- `erp_ai_agent_errors_total` - 錯誤計數 (Counter)
- `erp_ai_agent_requests_total` - 請求計數 (Counter)
- `erp_ai_agent_health_score` - 健康分數 (Gauge)
- `erp_agent_flow_executions_total` - 流程執行計數 (Counter)
- `erp_agent_flow_duration_seconds` - 流程執行時間 (Histogram)

---

## 四、可用工具清單 (40+ Tools)

### 4.1 庫存模組 (Inventory)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_inventory_search` | 庫存搜尋 | 搜尋 ERP 庫存資料 |
| `erp_low_stock_check` | 低庫存檢查 | 檢查低於安全庫存之料號 |
| `erp_sku_search` | 料號搜尋 | 搜尋料號主檔 |
| `erp_warehouse_search` | 倉庫搜尋 | 搜尋倉庫資料 |
| `erp_inventory_adjust` | 庫存調整 | 調整庫存數量 |
| `erp_warehouse_transfer` | 庫存調拨 | 建立庫存調拨單 |
| `erp_inventory_count` | 庫存盤點 | 執行庫存盤點 |
| `report_inventory` | 庫存報告 | 產生庫存報告 |

### 4.2 採購模組 (Procurement)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_po_search` | 採購訂單搜尋 | 搜尋採購訂單 |
| `erp_pr_search` | 採購申請搜尋 | 搜尋採購申請單 |
| `erp_supplier_search` | 供應商搜尋 | 搜尋供應商資料 |
| `erp_pr_create` | 建立採購申請 | 建立採購申請單 |
| `erp_po_create` | 建立採購訂單 | 建立採購訂單 |
| `erp_po_update` | 更新採購訂單 | 更新採購訂單 |
| `erp_goods_receipt_create` | 建立收貨單 | 建立收貨單 |
| `erp_supplier_rating` | 供應商評比 | 計算供應商評分 |

### 4.3 銷售模組 (Sales)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_so_search` | 銷售訂單搜尋 | 搜尋銷售訂單 |
| `erp_customer_search` | 客戶搜尋 | 搜尋客戶資料 |
| `erp_so_create` | 建立銷售訂單 | 建立銷售訂單 |
| `erp_price_update` | 更新價格 | 更新料號售價 |
| `erp_customer_credit_check` | 客戶信用檢核 | 檢查客戶信用額度 |
| `erp_delivery_create` | 建立出貨單 | 建立出貨單 |
| `erp_shipment_track` | 出貨追蹤 | 追蹤出貨狀態 |

### 4.4 生產模組 (Production)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_workorder_search` | 工單搜尋 | 搜尋生產工單 |
| `erp_workcenter_search` | 工作中心搜尋 | 搜尋工作中心 |
| `erp_wo_create` | 建立工單 | 建立生產工單 |
| `erp_wo_update` | 更新工單 | 更新工單狀態 |
| `erp_wo_complete` | 工單完工 | 標記工單完工 |
| `erp_material_issue` | 發料 | 工單發料 |
| `erp_production_efficiency` | 生產效率分析 | 分析生產效率 |
| `report_production` | 生產報告 | 產生生產報告 |

### 4.5 工程模組 (Engineering)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_bom_search` | BOM搜尋 | 搜尋物料清單 |
| `erp_routing_search` | 工藝搜尋 | 搜尋工藝路線 |
| `erp_operation_search` | 製程搜尋 | 搜尋製程資料 |
| `erp_bom_cost_rollup` | BOM成本計算 | 計算 BOM 各階成本 |

### 4.6 規劃模組 (Planning)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `mrp_run` | MRP執行 | 執行 MRP 運算 |
| `mrp_engine` | MRP引擎 | MRP 引擎 (AIPlanningService 內建) |
| `aps_scheduler` | APS排程器 | APS 排程器 |

### 4.7 人力資源模組 (HR)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_employee_search` | 員工搜尋 | 搜尋員工資料 |
| `erp_department_search` | 部門搜尋 | 搜尋部門資料 |
| `erp_attendance_search` | 考勤查詢 | 查詢員工考勤記錄 |
| `erp_leave_request_search` | 請假查詢 | 查詢請假申請 |
| `report_hr` | HR報告 | 產生人力資源報告 |

### 4.8 資產模組 (Assets)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_asset_search` | 資產搜尋 | 搜尋固定資產 |

### 4.9 財務模組 (Finance)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_cost_center_search` | 成本中心搜尋 | 搜尋成本中心 |
| `erp_bank_account_search` | 銀行帳戶搜尋 | 搜尋銀行帳戶 |
| `erp_loan_search` | 借款查詢 | 搜尋銀行借款 |
| `erp_investment_search` | 投資查詢 | 搜尋投資標的 |
| `erp_cost_calculate` | 成本計算 | 計算成本 |
| `report_finance` | 財務報告 | 產生財務報告 |

### 4.10 品質模組 (Quality)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `erp_iqc_report_search` | IQC報告查詢 | 搜尋 IQC 檢驗報告 |
| `erp_quality_complaint_search` | 品質異常查詢 | 搜尋品質異常單 |
| `report_quality` | 品質報告 | 產生品質報告 |

### 4.11 公用工具 (Utility)

| Tool ID | 名稱 | 功能 |
|---------|------|------|
| `calculator` | 計算機 | 執行數學運算 |
| `report_generator` | 報告產生器 | 產生各類報告 |
| `erp_approval_trigger` | 觸發審批 | 觸發審批流程 |
| `erp_notification_send` | 發送通知 | 發送通知 |

---

## 五、API 端點

### 5.1 PlanningController (`/api/planning`)

| 方法 | 路徑 | 功能 |
|------|------|------|
| POST | `/api/planning/plan` | 建立執行計畫 |
| POST | `/api/planning/execute` | 執行計畫 |
| POST | `/api/planning/review` | 審核結果 |
| GET | `/api/planning/kpi` | 取得 KPI 驗證 |
| GET | `/api/planning/health` | 取得健康狀態 |
| GET | `/api/planning/state/{executionId}` | 取得代理狀態 |
| GET | `/api/planning/history` | 取得執行歷史 |
| GET | `/api/planning/tools` | 取得可用工具清單 |
| GET | `/api/planning/graph/{planId}` | 取得規劃圖 |
| POST | `/api/planning/tool/execute` | 執行單一工具 |

### 5.2 AgentController (`/api/agent`)

| 方法 | 路徑 | 功能 |
|------|------|------|
| GET | `/api/agent/metrics-summary` | 取得指標摘要 |
| GET | `/api/agent/latency-history` | 取得延遲歷史 |
| GET | `/api/agent/qps-history` | 取得 QPS 歷史 |
| GET | `/api/agent/error-history` | 取得錯誤歷史 |
| GET | `/api/agent/flow-executions` | 取得流程執行記錄 |
| POST | `/api/agent/flows/{flowType}/trigger` | 觸發流程 |
| GET | `/api/agent/topology` | 取得系統拓撲 |

### 5.3 LlmController (`/api/llm`)

| 方法 | 路徑 | 功能 |
|------|------|------|
| POST | `/api/llm/complete` | 執行 LLM 推理 |
| GET | `/api/llm/model` | 取得目前模型名稱 |

---

## 六、資料模型

### 6.1 AgentExecution (資料庫模型)

```csharp
public class AgentExecution {
    Guid Id { get; set; }
    string ExecutionId { get; set; }        // 執行 ID (如 "EXEC-20260413120000")
    string PlanId { get; set; }             // 計畫 ID
    string Status { get; set; }             // pending/running/completed/failed
    decimal SuccessRate { get; set; }       // 成功率
    decimal Duration { get; set; }          // 執行時間(秒)
    string? ErrorMessage { get; set; }      // 錯誤訊息
    string? Context { get; set; }           // 執行上下文(JSON)
    string? Result { get; set; }            // 執行結果(JSON)
    DateTime StartedAt { get; set; }
    DateTime? CompletedAt { get; set; }
}
```

### 6.2 AgentTask (資料庫模型)

```csharp
public class AgentTask {
    Guid Id { get; set; }
    Guid AgentExecutionId { get; set; }
    string TaskId { get; set; }             // 子任務 ID
    string Description { get; set; }        // 描述
    string Status { get; set; }             // pending/success/failed
    string? ToolUsed { get; set; }          // 使用的工具
    string? Output { get; set; }            // 輸出結果
    string? Error { get; set; }             // 錯誤訊息
    decimal Duration { get; set; }          // 執行時間
    int Priority { get; set; } = 50         // 優先權
    string? Dependencies { get; set; }      // 依賴關係(JSON)
}
```

---

## 七、前端功能 (AI.tsx)

### 7.1 主要功能區塊

**1. 系統健康總覽**
- 可用率儀表板
- 無錯率儀表板
- 平均延遲顯示 (ms)
- Token 消耗 (24h)
- 活躍 Agent 數量

**2. Agent 健康分數**
- 異常派工 Agent (98.7%)
- 客服詢價 Agent (99.5%)
- 庫存預警 Agent (87.2%)

**3. 圖表監控**
- API 延遲趨勢圖 (P50/P95/P99)
- ERP QPS 圖
- 錯誤率趨勢圖

**4. Agent Flow 執行紀錄**
- 產線異常派工流程
- 客服詢價自動化流程
- 庫存預警補貨流程

**5. 系統拓撲圖**
- Agent 節點
- ERP 模組
- MES 系統
- LINE 官方帳號
- IoT 感測器

**6. AI 規劃測試區**
- 建立規劃
- 執行規劃
- 工具執行

### 7.2 可用 Hooks (`useApi.ts`)

```typescript
// Agent 監控
useAgentMetricsSummary()      // 指標摘要
useAgentLatencyHistory(hours) // 延遲歷史
useAgentQpsHistory(hours)     // QPS 歷史
useAgentErrorHistory(hours)   // 錯誤歷史
useAgentFlowExecutions(params) // 流程執行記錄
useTriggerAgentFlow()         // 觸發流程
useAgentTopology()            // 系統拓撲

// AI 規劃
usePlanningCreatePlan()       // 建立規劃
usePlanningExecutePlan()      // 執行規劃
usePlanningReview()           // 審核結果
usePlanningKpi()              // KPI 驗證
usePlanningHealth()           // 健康狀態
usePlanningTools()            // 可用工具
usePlanningToolExecute()      // 執行工具
```

---

## 八、設定配置

### 8.1 appsettings.json

```json
{
  "LLM": {
    "Provider": "openai",
    "ApiKey": "${OPENAI_API_KEY}",
    "BaseUrl": "https://api.openai.com/v1",
    "DefaultModel": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "TimeoutSeconds": 60
  },
  "ConnectionStrings": {
    "ErpGeneral": "Server=localhost;Database=ErpGeneral;..."
  }
}
```

### 8.2 環境變數

- `OPENAI_API_KEY` - OpenAI API Key
- 或使用 `Anthropic` Provider 並設定 `ANTHROPIC_API_KEY`

---

## 九、操作說明

### 9.1 使用前端測試區

1. 開啟瀏覽器進入 AI Agent 監控頁面 (`/ai`)
2. 捲動至頁面底部的「AI 規劃測試區」
3. **規劃標籤頁**：
   - 在「任務描述」輸入欄位輸入任務（如：「建立 100 個 A 產品的工單」）
   - 按下「建立規劃」按鈕
   - 系統會分析任務並產生子任務清單
   - 可選擇「執行規劃」來執行
4. **工具標籤頁**：
   - 選擇要測試的工具
   - 輸入 JSON 格式參數
   - 按下「執行工具」查看結果

### 9.2 直接呼叫 API

```bash
# 建立規劃
curl -X POST http://localhost:5100/api/planning/plan \
  -H "Content-Type: application/json" \
  -d '{"taskDescription": "建立工單", "context": "test"}'

# 執行工具
curl -X POST http://localhost:5100/api/planning/tool/execute \
  -H "Content-Type: application/json" \
  -d '{"toolId": "calculator", "parameters": {"expression": "10*10"}}'

# 取得監控指標
curl http://localhost:5100/api/agent/metrics-summary
```

---

## 十、風險管理

| 風險 | 嚴重性 | 對策 |
|------|--------|------|
| Prompt 不穩定 | 🔴 高 | 版本管理 + A/B 測試 |
| API 成本過高 | 🟡 中 | 快取機制 + 錯誤重試 |
| ERP 整合複雜度 | 🔴 高 | Mock API 先行 |
| 準確率未達標 | 🔴 高 | 持續學習 + 反饋機制 |

---

## 十一、相關檔案位置

| 類別 | 檔案路徑 |
|------|----------|
| 後端服務 | `ErpBackend/Services/AIPlanningService.cs` |
| 工具工廠 | `ErpBackend/Services/ToolFactory.cs` |
| LLM 服務 | `ErpBackend/Services/LlmService.cs` |
| ERP 整合 | `ErpBackend/Services/ErpIntegrationService.cs` |
| 規劃控制器 | `ErpBackend/Controllers/PlanningController.cs` |
| Agent 控制器 | `ErpBackend/Controllers/AgentController.cs` |
| 資料模型 | `ErpBackend/Models/System/SystemModels.cs` |
| 前端頁面 | `erp-frontend/src/pages/AI.tsx` |
| API 定義 | `erp-frontend/src/api/api.ts` |
| Hooks | `erp-frontend/src/hooks/useApi.ts` |
| 規格文件 | `docs/AI_Planning_Agent_Spec.md` |

---

*操作手冊版本: 1.0*
*更新日期: 2026-04-13*