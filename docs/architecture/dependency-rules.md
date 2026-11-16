# ERP 依賴規則矩陣 (Dependency Rules)

> 本文件定義各層級之間的依賴規則，由 Architecture Tests 自動驗證。

## 1. 依賴方向規則

| 來源 | 目標 | 允許 | 規則 |
|------|------|------|------|
| `Controllers` | `Services.Interfaces` | ✅ | 控制器只能依賴服務介面 |
| `Controllers` | `DTOs` | ✅ | 用於回應/請求模型 |
| `Controllers` | `Data.ErpGeneralDbContext` | ❌ | **禁止直接依賴 DbContext** |
| `Controllers` | `Models` | ⚠️ | 僅限簡單 CRUD，禁止複雜業務邏輯 |
| `Services` | `Data.ErpGeneralDbContext` | ✅ | 服務可存取資料 |
| `Services` | `Services.Interfaces` | ✅ | 可依賴其他服務介面 |
| `Services` | `Services.Impl` | ❌ | **禁止依賴其他服務實作類別** |
| `Models` | `DTOs` | ❌ | **領域模型不得依賴 DTO** |
| `Models` | `Controllers` | ❌ | **領域模型不得依賴控制器** |
| `Models` | `Data` | ❌ | **領域模型不得依賴資料層** |

## 2. 禁止依賴 (High Priority)

下列依賴關係必須在架構測試中 **明確禁止**：

```csharp
// ❌ 禁止：Controller 直接使用 DbContext
public class SkusController : BaseController
{
    private readonly ErpGeneralDbContext _db;  // 違規
}

// ✅ 允許：Controller 依賴服務介面
public class SkusController : BaseController
{
    private readonly ISkuService _skuService;  // 正確
}
```

```csharp
// ❌ 禁止：Service 依賴其他 Service 實作類別
public class AIPlanningService
{
    private readonly MrpEngine _mrpEngine;  // 違規（應依賴介面）
}

// ✅ 允許：依賴介面
public class AIPlanningService
{
    private readonly IMrpEngine _mrpEngine;  // 正確
}
```

## 3. 命名空間規則

| 規則 | 說明 |
|------|------|
| 控制器命名空間 | `ErpBackend.Controllers` |
| 服務介面命名空間 | `ErpBackend.Services` |
| 服務實作命名空間 | `ErpBackend.Services` |
| 領域模型命名空間 | `ErpBackend.Models.{Module}` |
| DTO 命名空間 | `ErpBackend.DTOs` |
| 資料存取命名空間 | `ErpBackend.Data` |

## 4. 目前違規現況 (Baseline)

| 違規類型 | 數量 | 說明 |
|----------|------|------|
| 控制器直接依賴 DbContext | 73 / 84 | 待 Phase 4 逐模組重構 |
| 服務未實作介面 | 8 個 | UomConversionService, ApprovalWorkflowService 等 |
| 大型服務 (>500 行) | 6 個 | AIPlanningService (923), MrpEngine (771), ToolFactory (920) |

## 5. 重構優先順序

依建議順序：**Inventory → Purchase → Sales → HR**

每個模組重構步驟：
1. 建立 `IXxxService` 介面
2. 建立 `XxxService` 實作（搬移業務邏輯）
3. 控制器改為依賴介面
4. 於 `Program.cs` 註冊 DI
5. 執行架構測試驗證通過
6. 執行單元測試確保無回歸

