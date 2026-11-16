# 核心模組 API 端點 (Endpoints)

> 來源：`src/docs/05_API_Specifications.md`

## 1. 端點一覽

### 1.1 Dashboard

| 方法 | 端點 | 說明 |
|------|------|------|
| GET | `/api/v1/erp/dashboard/sidebar-badges` | 側邊欄徽章（待辦事項統計） |

### 1.2 Inventory

| 方法 | 端點 | 說明 |
|------|------|------|
| GET | `/api/v1/erp/inventory/transactions` | 查詢庫存異動軌跡 |

### 1.3 Sales / Procurement

| 方法 | 端點 | 說明 |
|------|------|------|
| POST | `/api/v1/erp/procurement/orders/{id}/receive` | 收料並驗證允收容差 |

### 1.4 MES / APS

| 方法 | 端點 | 說明 |
|------|------|------|
| POST | `/api/v1/erp/manufacturing/wip` | 提報在製品進度 |
| POST | `/api/v1/erp/aps-integration/import` | 接收 APS 引擎排程結果 |

### 1.5 R&D

| 方法 | 端點 | 說明 |
|------|------|------|
| POST | `/api/v1/erp/ecns` | 建立工程變更 |

### 1.6 Finance

| 方法 | 端點 | 說明 |
|------|------|------|
| POST | `/api/v1/erp/workorders/{id}/costing/settle` | 實際成本結算 |

## 2. 控制器清單（後端）

後端位於 `src/ErpBackend/Controllers/`，包含以下模組控制器（共 76 個）：

| 模組 | 控制器 |
|------|--------|
| 認證授權 | AuthController, UsersController, SystemSettingsController |
| 庫存 | InventoryController, SkusController, BarcodeController, BinLocationsController, SubstituteManagement |
| 銷售 | SalesOrdersController, QuotesController, CustomersController, DeliveryOrdersController, CashReceiptsController |
| 採購 | PurchaseRequisitionsController, SuppliersController |
| 製造 | BomsController, RoutingsController, WorkOrders, CompletionOrdersController |
| APS/MPS | ApsController, ApsIntegrationController, MpsController, AiSchedulingController |
| AI | AiController, AgentController, AiDecisionController, AiPlanning |
| 財務 | FinanceController, AccountsPayableController, BudgetController, CostCentersController, CostElementsController, CostPostingsController |
| 人事 | EmployeesController, DepartmentsController, AttendanceController, LeaveRequestsController |
| 資產 | AssetsController, EquipmentController |
| 協作 | CollaborationController, CalendarController |
| ESG | EsgController, CarbonController, DigitalController |
| 其他 | DashboardController, CurrenciesController, AllocationController, ApprovalWorkflowController, ActivityTypesController, EdiController, EcnsController, CostingController, ... |

> 完整清單請參考 `src/ErpBackend/Controllers/` 目錄。

