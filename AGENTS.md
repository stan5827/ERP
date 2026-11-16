# AGENTS.md — ERP 專案 AI Agent 協作指南

> 本文件為 AI Agent（含 Claude Code、Codex、Cursor、GitHub Copilot 等）於此 repo 內的**強制作業規範**。
> 目標：讓任何 AI Agent 在最小上下文下，能正確、一致地貢獻此專案。

---

## 1. 啟動須知（Read First）

### 1.1 專案概覽
| 區域 | 路徑 | 技術 |
|------|------|------|
| 後端 API | `src/ErpBackend/` | ASP.NET Core 10, EF Core, PostgreSQL |
| 前端 Web | `src/erp-frontend/` | React 19, TypeScript, Vite |
| 行動 App | `src/erp-employee-app/` | React Native, Expo |
| 文件產生腳本 | `tools/scripts/` | Node.js |
| 單元測試 | `tests/ErpBackend.Tests/` | xUnit |
| 整合測試 | `tests/ErpBackend.IntegrationTests/` | xUnit |
| 部署 | `deployment/` | Docker Compose |
| 文件 | `docs/`, `src/docs/` | Markdown |
| 架構規範 | `docs/architecture/` | Markdown |

### 1.2 預設分支
- 主要分支：`master`（既有） / `main`（CI 亦支援）

### 1.3 建置與測試指令
```bash
# 後端建置
dotnet build src/ErpBackend/ErpBackend.csproj

# 執行所有測試
dotnet test tests/ErpBackend.Tests/ErpBackend.Tests.csproj

# 僅架構測試
dotnet test tests/ErpBackend.Tests/ErpBackend.Tests.csproj --filter "FullyQualifiedName~Architecture"

# 前端建置
cd src/erp-frontend && npm ci && npm run build
```

---

## 2. 架構規範（非遵守不可）

### 2.1 分層與依賴方向
```
Controller (Presentation, Controllers/)
      │ 依賴介面
      ▼
Service (Application, Services/)  ← 業務邏輯核心
      │ 依賴模型
      ▼
Model (Domain, Models/)
      ▲
      │ 實作
Infrastructure (Data/ = ErpGeneralDbContext)
```

### 2.2 鐵律（Hard Rules）
1. **控制器不得注入 `ErpGeneralDbContext`** — 一律透過 Service 介面
2. **服務不得依賴控制器命名空間** — `Services/` 不得 `using ErpBackend.Controllers`
3. **模型層純潔** — `Models/` 不得引用 Controllers / DTOs / Data
4. **非同步一致** — 回傳 `Task` / `Task<T>` 的方法一律以 `Async` 結尾
5. **介面分離** — 每個公開服務類別應有 `IXxxService` 介面
6. **禁止阻塞呼叫** — 不得使用 `.Result`、`.Wait()`、`Task.Run(...).GetAwaiter().GetResult()`
7. **禁止上帝類** — 單檔不得超過 500 行（超過應拆分）

### 2.3 命名慣例
| 類型 | 慣例 | 範例 |
|------|------|------|
| 介面 | `I` + 名稱 + `Service` | `IInventoryService` |
| 服務 | 名稱 + `Service` | `InventoryService` |
| 控制器 | 名稱 + `Controller` | `InventoryController` |
| DTO | 名稱 + `Dto` | `WarehouseDto` |
| 非同步方法 | 名稱 + `Async` | `GetSkuBalanceAsync` |

---

## 3. 工作流程（Agent 執行任務步驟）

### 3.1 例行任務
1. **先讀取** `AGENTS.md`、`docs/architecture/standards.md`、`docs/architecture/dependency-rules.md`
2. 使用 `search_files` / `read_file` 定位相關檔案，**先了解再修改**
3. 修改後**必執行** `dotnet build`（後端）確保 0 errors
4. 涉及依賴方向的改動，執行架構測試驗證

### 3.2 新增 Controller 流程
```
1. 建立 service interface (I{模組}Service) in Services/
2. 實作 service class ({模組}Service) : I{模組}Service
3. 註冊 DI Builder in Program.cs: AddScoped<I{模組}Service, {模組}Service>()
4. 建立 controller，注入 interface
5. 執行 dotnet build + 架構測試
```

### 3.3 重構既有 Controller（移除 DbContext）
```
1. 檢查該 controller 使用了哪些 DbSet
2. 於 Services/ 建立對應的 service + interface，搬移查詢邏輯
3. 於 Program.cs 註冊
4. 修改 controller 改注入 interface
5. 移除 ErpBackend.Data / EntityFrameworkCore using
6. 執行 dotnet build + 架構測試
```

---

## 4. 技術債與 Skip 測試
- `tests/ErpBackend.Tests/Architecture/` 中有數個 `[Fact(Skip = "...")]` 測試
- 代表**已知技術債**（如 DTO 未統一命名、控制器仍直接使用 DbContext）
- **勿隨意移除 Skip**，應先完成對應重構再解除
- 進度追蹤：`TODO.md`

---

## 5. 品質門檻
- Quality Gate 規則：`docs/architecture/quality-gate.md`
- 合併前必須：
  - ✅ `dotnet build` 0 errors
  - ✅ `dotnet test` 0 failed
  - ✅ 架構測試 0 failed（skipped 需有登記）
  - ✅ 符合 `.editorconfig` 風格

---

## 6. 環境與機密
- 不可提交 `.env`、`*.key`、`*.pfx`、`*.cer`（見 `.gitignore`）
- 連線字串、JWT 金鑰、LLM API Key 一律透過環境變數或 `appsettings.json`（不入庫）
- LLM 相關設定：`Configs/llm_config.json`、`Configs/ai_planning_config.json`

---

## 7. AI Agent 使用本檔的建議
- **首次**進入 repo：完整讀取本檔 + `docs/architecture/standards.md`
- **小任務**：直接依本檔規範執行，無需再讀所有架構文件
- **跨模組任務**：讀取 `docs/architecture/module-map.md` 了解模組邊界
- **新增依賴套件**：需確認與 .NET 10 相容，並於 csproj 聲明

---

## 8. 目錄結構速覽
```
ERP/
├── src/
│   ├── ErpBackend/          # .NET 10 Web API
│   │   ├── Controllers/     # API 層（僅依賴 Service 介面）
│   │   ├── Services/        # 業務邏輯（含介面）
│   │   ├── Models/          # Domain 模型
│   │   ├── DTOs/            # 傳輸物件
│   │   ├── Data/            # EF Core DbContext
│   │   └── Program.cs       # DI 註冊
│   ├── erp-frontend/        # React 前端
│   ├── erp-employee-app/    # React Native 行動端
│   └── docs/                # 業務/功能文件
├── tests/
│   ├── ErpBackend.Tests/    # 單元測試（含 Architecture/）
│   └── ErpBackend.IntegrationTests/
├── deployment/              # Docker / Prometheus / Grafana
├── docs/                    # 工程文件（API/DB/架構）
└── tools/scripts/           # 文件產生腳本
```
