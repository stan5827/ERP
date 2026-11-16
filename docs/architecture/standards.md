# ERP 架構標準 (Architecture Standards)

> 本文件定義 ERP 系統之架構原則、分層規範與程式碼標準，作為所有開發與重構之依據。

## 1. 架構原則

| 原則 | 說明 | 驗證方式 |
|------|------|----------|
| **依賴倒轉 (DIP)** | 高階模組不依賴低階模組，兩者皆依賴抽象 | Architecture Tests |
| **介面隔離 (ISP)** | 服務介面應小而專注，避免巨型介面 | Code Review |
| **單一職責 (SRP)** | 每個類別只負責單一職責 | Architecture Tests |
| **分層架構 (Layered)** | Controllers → Services → Repositories → DbContext | Architecture Tests |
| **明確依賴** | 依賴關係由建構子注入 (DI) | Architecture Tests |
| **無循環依賴** | 模組間不得相互依賴 | Architecture Tests |

## 2. 分層架構 (Clean Architecture)

```
┌─────────────────────────────────────────────┐
│  Presentation Layer (Controllers)           │
│  → 僅負責 HTTP 請求/回應                     │
│  → 依賴 Interfaces（不得直接依賴 DbContext）  │
├─────────────────────────────────────────────┤
│  Application Layer (Services)               │
│  → 業務邏輯核心                              │
│  → 實現服務介面                              │
│  → 可依賴 Models & DTOs                      │
├─────────────────────────────────────────────┤
│  Domain Layer (Models)                      │
│  → 領域模型 / 實體                           │
│  → 無外部依賴                                │
├─────────────────────────────────────────────┤
│  Infrastructure Layer (Data / Repositories) │
│  → DbContext / Repository 實作               │
│  → 外部服務整合                              │
└─────────────────────────────────────────────┘
```

### 2.1 依賴規則

| 依賴方向 | 允許 | 禁止 |
|----------|------|------|
| Controller → Service | ✅ | ❌ Controller → DbContext |
| Controller → Interface | ✅ | ❌ Controller → Repository 實作 |
| Service → Interface | ✅ | ❌ Service → 其他 Service 實作 |
| Service → Model | ✅ | ❌ Model → Controller |
| Repository → DbContext | ✅ | ❌ Model → DTO |

## 3. 命名慣例

| 項目 | 慣例 | 範例 |
|------|------|------|
| 介面 | `I` 前綴 + PascalCase | `ISkuService` |
| 服務類別 | PascalCase + `Service` 後綴 | `SkuService` |
| 控制器 | PascalCase + `Controller` 後綴 | `SkusController` |
| DTO | PascalCase + `Dto` 後綴 | `SkuDto` |
| 測試類別 | 目標類別 + `Tests` 後綴 | `SkuServiceTests` |
| 非同步方法 | `Async` 後綴 | `GetSkusAsync` |
| 私有欄位 | `_camelCase` | `_db` |

## 4. 程式碼標準

- **非同步**：所有 I/O 操作使用 `async/await`，禁止 `.Result` / `.Wait()` 阻塞呼叫。
- **空值處理**：啟用 `Nullable` 參考型別，使用 `?` 明確標示可空。
- **例外處理**：Controller 使用全域例外處理中間件，Service 拋出具語意的例外。
- **LINQ**：資料庫查詢應在查詢端完成篩選/排序/分頁，避免先載入再篩選。
- **DTO 映射**：使用明確映射方法（如 `MapToDto`），避免 AutoMapper 隱式魔法（除非已導入）。

## 5. 目錄結構

```
src/ErpBackend/
├── Controllers/       # API 層（只做 HTTP 轉換）
├── Services/          # 業務邏輯層（含 Interface）
├── Repositories/      # 資料存取層（目標：逐步導入）
├── Models/            # 領域模型
│   ├── Inventory/
│   ├── HR/
│   ├── Procurement/
│   ├── Sales/
│   └── ...
├── DTOs/              # 資料傳輸物件
├── Data/              # DbContext 與 Migrations
├── Middleware/        # 中間件
└── Filters/           # Action Filters
```

## 6. 模組劃分

| 模組 | 主要目錄 | 控制器前綴 |
|------|----------|------------|
| Inventory 庫存 | `Models/Inventory` | `Inventory*` |
| Purchase 採購 | `Models/Procurement` | `Purchase*` / `Supplier*` |
| Sales 銷售 | `Models/Sales` | `Sales*` / `Customer*` |
| HR 人力資源 | `Models/HR` | `Employee*` / `Leave*` |
| Manufacturing 製造 | `Models/MES` | `WorkOrder*` / `Bom*` |
| Finance 財務 | `Models/Finance` | `Finance*` / `Cost*` |

## 7. 驗收標準

- [x] 後端建置無錯誤、無警告（`dotnet build` 0 errors / 0 warnings）
- [ ] 架構測試全部通過（Phase 2）
- [ ] 控制器不直接依賴 DbContext（Phase 4 重構後）
- [ ] 所有服務實作介面（Phase 4 重構後）

