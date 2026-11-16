# ERP 專案接手指南

本文依目前倉庫結構與架構文件整理，提供可循著程式碼驗證的閱讀路徑。文件中的統計數字請視為參考，而非最新實測。

## 1. 專案全貌與架構梳理

### 一句話認識系統

這是由 **ASP.NET Core 8、EF Core、PostgreSQL** 組成的 ERP API，搭配 **React／TypeScript Web 前端**、**Expo 員工 App**，另有供應商入口及 Docker 部署、監控設定。架構文件將後端描述為 Controller、Service、Model、Infrastructure 分層；實際依賴方式仍應逐條沿著程式碼確認。

### 建議閱讀順序

1. **先看專案地圖與規範**
   - [AGENTS.md](../AGENTS.md)：專案目錄、測試方式與開發規則。
   - [overview.md](./architecture/overview.md)：技術棧和整體部署概念。
   - [module-map.md](./architecture/module-map.md)：業務模組、API 範例及重構現況。
   - [standards.md](./architecture/standards.md) 和 [dependency-rules.md](./architecture/dependency-rules.md)：預期分層與依賴方向。

2. **看後端如何啟動與接線**
   - [Erp.sln](../Erp.sln)：Solution 中有哪些專案。
   - [ErpBackend.csproj](../src/ErpBackend/ErpBackend.csproj)：目標框架與 NuGet 相依。
   - [Program.cs](../src/ErpBackend/Program.cs)：資料庫、DI、認證、Middleware、路由如何註冊。
   - [Controllers/](../src/ErpBackend/Controllers/) 和 [Services/](../src/ErpBackend/Services/)：API 入口與業務處理的位置。

3. **看各客戶端如何呼叫 API**
   - [App.tsx](../src/erp-frontend/src/App.tsx)：頁面及路由入口。
   - [前端 API 目錄](../src/erp-frontend/src/api/)：依業務模組分組的 API 呼叫。
   - [client.ts](../src/erp-frontend/src/api/client.ts)：API base URL、認證標頭與錯誤處理。
   - [員工 App](../src/erp-employee-app/) 及 [供應商入口](../src/erp-supplier-portal/)：另兩個客戶端入口。

4. **再追資料模型與資料庫**
   - [ErpGeneralDbContext.cs](../src/ErpBackend/Data/ErpGeneralDbContext.cs)：EF Core 的 `DbSet` 與關聯設定。
   - [Models/](../src/ErpBackend/Models/)：按 Inventory、HR、MES、Finance 等領域分類的實體。
   - [Migrations/](../src/ErpBackend/Migrations/)：資料庫結構演進；比較 Migration 與 Model，可了解表格何時增刪欄位或關係。

5. **最後看測試、部署與品質門檻**
   - [tests/](../tests/)：後端單元測試、整合測試。
   - [docker-compose.yml](../deployment/docker-compose.yml)：本地或部署用的資料庫、API、前端及監控服務。
   - [quality-gate.md](./architecture/quality-gate.md)：文件記載的建置、測試和技術債門檻。

### 用庫存功能畫出資料流

```text
Inventory 頁面
  → 前端 API 函式
  → Axios client（base URL、認證、錯誤處理）
  → InventoryController
  → IInventoryService / InventoryService
  → ErpGeneralDbContext
  → PostgreSQL
```

可以用 `GET /api/erp/inventory/balance/{skuCode}` 作為第一條追蹤路徑：從 [inventory.ts](../src/erp-frontend/src/api/inventory.ts) 找呼叫，再到 [InventoryController.cs](../src/ErpBackend/Controllers/InventoryController.cs)、[InventoryService.cs](../src/ErpBackend/Services/InventoryService.cs)，最後查 [InventoryModels.cs](../src/ErpBackend/Models/Inventory/InventoryModels.cs) 和 [ErpGeneralDbContext.cs](../src/ErpBackend/Data/ErpGeneralDbContext.cs) 中的實體及設定。

### 注意文件統計可能落後

架構文件中的控制器數字彼此不一致：[overview.md](./architecture/overview.md) 記載 76 個，[module-map.md](./architecture/module-map.md) 記載 84 個；目前 [Controllers/](../src/ErpBackend/Controllers/) 目錄實際列出 96 個 `.cs` 檔案。這些檔案數不等於有效 API 數量，建議以目前路由與程式碼重新盤點。文件中的重構狀況、覆蓋率等數字也應先核對日期與最新測試結果。

## 2. 環境建置與快速啟動

### 步驟清單

1. **確認工具版本**
   ```powershell
   dotnet --info
   dotnet --list-sdks
   node --version
   npm --version
   docker version
   docker compose version
   ```
   後端目標為 .NET 8，前端及員工 App 各有獨立的 `package-lock.json`。此倉庫未找到 `global.json`，因此請確認本機安裝了相容的 .NET SDK。

2. **先檢查設定檔，不要覆蓋現有機密**
   - [後端 .env 範本](../src/ErpBackend/.env.example) 是後端環境變數範本；`Program.cs` 會呼叫 `Env.Load()`，並要求資料庫連線字串及至少 32 字元的 JWT key。
   - [docker-compose.yml](../deployment/docker-compose.yml) 包含 PostgreSQL 16、Redis 7、後端、前端、Prometheus 和 Grafana。完整啟動需要 `POSTGRES_PASSWORD`、`JWT_SECRET_KEY`、`GRAFANA_ADMIN_PASSWORD` 等環境設定。
   - 倉庫根目錄目前有 `.env`。不要直接輸出、貼上或覆蓋它；先確認本機設定是否齊全，也不要提交 `.env` 或真實金鑰。

3. **選擇一種啟動方式**

   **完整 Docker 環境：**
   ```powershell
   docker compose --env-file .env -f deployment/docker-compose.yml up -d --build
   docker compose --env-file .env -f deployment/docker-compose.yml ps
   Invoke-RestMethod http://localhost:5100/health
   ```
   Compose 對外提供後端 `5100`、前端 `5173`。健康檢查路徑會確認後端及資料庫連線。此 Compose 設定將後端設為 Production，因此 Swagger 不會像 Development 模式那樣啟用。

   **Docker 跑資料服務、前後端在本機開發：**
   ```powershell
   docker compose --env-file .env -f deployment/docker-compose.yml up -d postgres redis

   dotnet restore Erp.sln
   dotnet build src/ErpBackend/ErpBackend.csproj

   Push-Location src/ErpBackend
   dotnet run --launch-profile http
   ```
   後端 HTTP profile 使用 `localhost:5100`。另開一個 PowerShell 視窗啟動前端：
   ```powershell
   Push-Location src/erp-frontend
   npm ci
   npm run dev
   ```
   Vite 預設將 `/api` 代理到 `http://localhost:5100`；若修改後端埠號，請同步調整 Vite 設定或 `ERP_API_PROXY_TARGET`。

   如果 Compose 在只啟動資料服務時，仍因其他服務的必要環境變數而報錯，先補齊 Compose 所需變數，再重試；不要以真實機密值貼到聊天或終端記錄中。

4. **準備資料庫**
   - 啟動後先確認健康檢查與連線，再檢視 [Migrations/](../src/ErpBackend/Migrations/)。
   - 若本機資料庫尚未套用 Migration，確認目標是可拋棄的開發資料庫後，再依專案所用的 EF 工具套用；不要對不確定的資料庫直接執行更新。

5. **執行測試或啟動 App**
   ```powershell
   dotnet test tests/ErpBackend.Tests/ErpBackend.Tests.csproj
   Push-Location src/erp-frontend
   npm test
   ```
   員工 App 使用 [package.json](../src/erp-employee-app/package.json) 中的 Expo scripts；依目標平台安裝依賴及啟動前，確認 Expo 對應的 Node 與原生相依版本。

### 預防常見相依性問題

- 使用 `npm ci`，不要在同一專案混用 `npm install`、其他套件管理器或不同 lockfile。
- Web、員工 App、供應商入口是不同套件專案；在各自目錄安裝依賴，不要只看根目錄的 lockfile。
- 不要同時啟動 Docker 前端與本機 Vite 前端；兩者都可能使用 `5173`。同理，確認後端 `5100` 未被其他程序占用。
- 新增或升級套件前，先核對 React、Expo、TypeScript、EF Core 與目標執行環境版本，再更新對應 lockfile。
- JWT key、資料庫密碼及 API key 使用本機環境變數或密碼管理工具；範本值不可當作正式機密。

### 前端與 Docker 的 Port 對應

| 前端 | 資料夾與技術 | 本機開發 Port | API 指向 |
|---|---|---:|---|
| ERP 內部網頁 | [src/erp-frontend/](../src/erp-frontend/)；React 19、TypeScript、Vite | 預設 `5173`，可用 `ERP_FRONTEND_PORT` 覆寫 | 呼叫 `/api/erp`；Vite 將 `/api` 代理到 `http://localhost:5100`，可用 `ERP_API_PROXY_TARGET` 覆寫。 |
| 員工 App | [src/erp-employee-app/](../src/erp-employee-app/)；React Native、Expo、TypeScript | Expo 開發伺服器預設使用 `8081`；不是 Docker Compose 服務 | [config.ts](../src/erp-employee-app/src/constants/config.ts) 設為 `http://10.0.2.2:5100/api/erp`，供 Android Emulator 連回開發主機；實體手機需改用主機可連線的 LAN IP。 |
| 供應商入口 | [src/erp-supplier-portal/](../src/erp-supplier-portal/)；React、TypeScript、Vite，使用 OIDC/Keycloak | 預設 `5176`，可用 `SUPPLIER_PORTAL_PORT` 覆寫 | 呼叫 `/api/supplier-portal`；Vite 將此路徑代理到 `http://localhost:5100`，可用 `SUPPLIER_PORTAL_API_PROXY_TARGET` 覆寫。 |

[deployment/docker-compose.yml](../deployment/docker-compose.yml) 只將 ERP 網頁前端列為 `frontend` 服務，並設定主機 `5173` 對應容器 Nginx `80`（`5173:80`）。後端則是主機 `5100` 對應容器 `8080`（`5100:8080`）。員工 App 和供應商入口不在這份 Compose 的服務清單內。

本機開發時，ERP 網頁與供應商入口由各自的 Vite proxy 將 API 請求轉送至 `localhost:5100`。Docker 執行網頁前端時，[nginx.conf](../deployment/nginx.conf) 將 `/api/` 轉送至 Compose 網路內的 `http://backend:8080/api/`；瀏覽器開啟 `http://localhost:5173`，不需要直接呼叫容器內部位址。員工 App 則直接依自身 API URL 呼叫後端。

各前端可分別啟動：

```powershell
# ERP 網頁前端
Set-Location src/erp-frontend
npm ci
npm run dev

# 供應商入口（另開終端機）
Set-Location src/erp-supplier-portal
npm ci
npm run dev

# 員工 App（另開終端機）
Set-Location src/erp-employee-app
npm ci
npm start
```

員工 App 可使用 `npm run android` 啟動 Android Emulator。供應商入口除 API 外，亦需要有效的 Keycloak/OIDC 公開設定，細節見 [src/erp-supplier-portal/.env.example](../src/erp-supplier-portal/.env.example)。Docker Compose 中的 `5173` 是主機映射埠，而本機 Vite 的 `5173` 是開發伺服器埠；同時啟動兩者時會發生埠號衝突。

### 檢視 EF Core Schema 與安全更新資料庫

Migration 是依序記錄的資料庫變更指令；資料庫已套用到哪個版本，則由該資料庫自己的 Migration 歷史決定。因此必須分開確認「程式有什麼 Migration」、「目前 Model 和 Snapshot 是否一致」以及「目標 PostgreSQL 實際套用到哪裡」。

#### 1. 解讀 Migration 歷史與預期 Schema

- 在 [Migrations/](../src/ErpBackend/Migrations/) 依 Migration ID（通常包含時間戳及描述名稱）排序，閱讀 `Up()` 中的 `CreateTable`、`AddColumn`、`CreateIndex`、`AddForeignKey`、`DropColumn` 等操作，掌握 schema 演進；若有 `migrationBuilder.Sql(...)`，也要檢查其資料更新效果。Migration 名稱只是摘要，實際行為以 `Up()` 為準。
- `Down()` 描述反向操作，可能刪除資料或結構，不應視為必然可無損回復。
- [ErpGeneralDbContext.cs](../src/ErpBackend/Data/ErpGeneralDbContext.cs) 的 `DbSet<T>` 列出實體；`OnModelCreating` 描述索引、外鍵、欄位限制及刪除行為。可與 [Models/](../src/ErpBackend/Models/) 中的實體類別互相比對。
- [ErpGeneralDbContextModelSnapshot.cs](../src/ErpBackend/Migrations/ErpGeneralDbContextModelSnapshot.cs) 保存建立最新 Migration 時的模型快照，是 EF Core 比較新舊 Model 的基準；它不是連線中資料庫的即時 schema。
- 比對「目前 Model → Snapshot」可發現尚未產生 Migration 的模型差異；比對「最新 Migration → 資料庫 Migration 歷史」可判斷資料庫是否落後。

#### 2. 檢查資料庫目前版本與程式最新版本

先確認 EF CLI 及連線目標。專案使用 .NET 8 和 EF Core Tools 8.0.11；如尚未安裝相容版本：

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
```

若已安裝但版本不同，使用：

```powershell
dotnet tool update --global dotnet-ef --version 8.0.11
```

在倉庫根目錄執行以下命令，列出 Migration 並與連線資料庫的套用狀態比對：

```powershell
dotnet restore Erp.sln
dotnet ef migrations list `
  --project src/ErpBackend/ErpBackend.csproj `
  --startup-project src/ErpBackend/ErpBackend.csproj `
  --context ErpGeneralDbContext
```

檢查輸出中的最新 Migration 是否已套用，且沒有尚未套用的 Migration。亦可查 PostgreSQL 歷史表：

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
```

若最新 Migration ID 與資料庫歷史表中最新 ID 相同，表示歷史紀錄顯示資料庫已套用到目前 Migration；若程式中有較新的 Migration 尚未標示套用，資料庫即落後。若 `__EFMigrationsHistory` 不存在，先確認連線是否指向正確資料庫，以及該資料庫是否曾由 EF Core 初始化；不要手動建立或修改歷史紀錄來掩蓋差異。

檢查 Model 是否有尚未遷移的變更，可執行：

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src/ErpBackend/ErpBackend.csproj `
  --startup-project src/ErpBackend/ErpBackend.csproj `
  --context ErpGeneralDbContext
```

這個檢查比較目前 Model 與 Snapshot，**不會**取代資料庫 Migration 套用狀態檢查。若命令顯示有 pending model changes，先釐清是否漏建 Migration，不要只因資料庫套用到既有最新 Migration 就認定 schema 完全一致。

#### 3. 安全初始化或更新本機資料庫

1. **確認連線是本機資料庫。** [Program.cs](../src/ErpBackend/Program.cs) 由 `DB_CONNECTION_STRING` 或設定檔取得連線，並載入 `.env`。確認實際使用的資料庫名稱、主機和埠號；不要把連線字串、密碼放入共享記錄或提交到版本控制。
2. **空白本機資料庫初始化。** 確認目標資料庫確實是新建且可由本專案管理後，`database update` 會建立 schema 並套用全部 Migration：
   ```powershell
   dotnet ef database update `
     --project src/ErpBackend/ErpBackend.csproj `
     --startup-project src/ErpBackend/ErpBackend.csproj `
     --context ErpGeneralDbContext
   ```
3. **已有資料的本機資料庫先備份。** 若 PostgreSQL 使用 Docker Compose 並將主機 `5432` 映射至容器 `5432`，依實際資料庫及使用者調整：
   ```powershell
   pg_dump -Fc -h localhost -p 5432 -U erp -d ErpGeneral -f .\ErpGeneral-before-migration.dump
   ```
   確認備份檔存在且可安全保存，不要提交到 Git；重要資料應依團隊程序驗證備份可還原。
4. **產生並審閱 SQL。** 此命令只產生腳本，不會自行套用：
   ```powershell
   dotnet ef migrations script --idempotent `
     --project src/ErpBackend/ErpBackend.csproj `
     --startup-project src/ErpBackend/ErpBackend.csproj `
     --context ErpGeneralDbContext `
     --output .\migration-review.sql
   ```
   審閱刪除欄位／資料表、欄位型別變更、索引與外鍵調整、資料回填及可能鎖表的操作。若資料庫已有資料但 Migration 歷史缺失，停止操作並先釐清 schema 與歷史，不要直接更新或手動補寫 Migration 歷史。
5. **套用已審閱的最新 Migration。** 再次核對目標資料庫後執行：
   ```powershell
   dotnet ef database update `
     --project src/ErpBackend/ErpBackend.csproj `
     --startup-project src/ErpBackend/ErpBackend.csproj `
     --context ErpGeneralDbContext
   ```
6. **驗證更新結果。** 重新執行 `dotnet ef migrations list` 並查詢 `__EFMigrationsHistory`，確認最新 Migration 已套用；再執行：
   ```powershell
   dotnet build src/ErpBackend/ErpBackend.csproj
   ```
   若後端已啟動，亦可檢查 `http://localhost:5100/health` 的資料庫連線狀態。

`dotnet ef database update` 會直接修改啟動專案所取得連線字串指向的資料庫。正式或共享資料庫必須先走團隊核准、備份、SQL 審查及部署流程；不可將本機操作命令直接套用到正式環境。不要以 `Down()` 或回退 Migration 取代已驗證的備份與復原程序，因為反向操作可能造成不可逆資料遺失。

## 3. 核心程式碼、業務邏輯與資料庫

### 建議追查順序

1. **從 API 路由開始**：找 Controller 的 `[Route]`、`[HttpGet]`、`[HttpPost]`，記錄 HTTP method、路徑、輸入 DTO 和回應型別。
2. **追到 Service**：看 Controller 呼叫哪個 Service 介面、方法內如何驗證、查詢、改寫資料，以及是否有交易處理。
3. **追到 Entity 和 `DbContext`**：由 `DbSet` 找實體，再看主鍵、外鍵、導覽屬性、索引、刪除行為及 `OnModelCreating`。
4. **對照 Migration**：確認資料庫目前 Schema 與程式模型差異；留意欄位是否 nullable、預設值、唯一索引和資料回填。
5. **回到前端確認契約**：檢查 URL、query/body 欄位、JSON 命名、錯誤及分頁格式是否與 API 一致。
6. **用測試確認行為**：測試通常能補足錯誤路徑、權限、狀態轉換等文件未說明的規則。

### 快速搜尋範例

在倉庫根目錄使用 `rg`：

```powershell
rg -n "\[Route|\[Http(Get|Post|Put|Delete|Patch)" src\ErpBackend\Controllers
rg -n "DbSet<|OnModelCreating|HasForeignKey|HasOne|HasMany" src\ErpBackend\Data
rg -n "SaveChangesAsync|BeginTransaction|CommitAsync|RollbackAsync" src\ErpBackend
rg -n "Authorize|Permission|Approval|Status|Workflow" src\ErpBackend\Controllers src\ErpBackend\Services
```

常見線索：`HasForeignKey`／`HasOne` 查實體關聯；`SaveChangesAsync` 查資料寫入；`BeginTransaction` 查交易範圍；`Approval`、`Status`、`Workflow` 查簽核與狀態變化；`[Authorize]`、`Permission` 查存取控制。

## 4. 品質與潛在風險評估

| 檢查面向 | 檢查方法 | 目前可注意的訊號 |
|---|---|---|
| 建置與警告 | 執行 `dotnet build`；記錄新增 warning 是否被忽略 | [Directory.Build.props](../Directory.Build.props) 設定 `TreatWarningsAsErrors=false`，所以建置成功不代表沒有警告。 |
| 測試與覆蓋率 | 跑後端單元／整合測試和前端 Vitest；檢查跳過測試、測試報告與實際 coverage | [quality-gate.md](./architecture/quality-gate.md) 記載低覆蓋率基線及多項 Skip 技術債；這是文件中的狀態，並非本次重新執行的測量結果。 |
| 架構依賴 | 搜尋 Controller 是否直接使用 DbContext；查看 Service 是否依賴 Controller | [dependency-rules.md](./architecture/dependency-rules.md) 記載舊基線有大量 Controller 直接依賴 DbContext；應以目前程式碼及架構測試確認。 |
| 錯誤處理與日誌 | 追蹤例外如何轉成 HTTP 回應、日誌是否包含足夠上下文，並避免記錄密碼、Token 等機密 | [Program.cs](../src/ErpBackend/Program.cs) 註冊全域錯誤處理、請求日誌和 Metrics；進一步檢查 [Middleware/](../src/ErpBackend/Middleware/) 的實作及敏感資料遮蔽。 |
| 依賴版本與安全更新 | 查核 NuGet 易受攻擊／過期套件，並在各 npm 專案執行 `npm audit` | 以專案各自的 `.csproj`、`package.json` 和 lockfile 為準，不要只看架構文件中的版本。 |

可使用以下指令做初步檢查：

```powershell
dotnet test tests/ErpBackend.Tests/ErpBackend.Tests.csproj
dotnet test tests/ErpBackend.IntegrationTests/ErpBackend.IntegrationTests.csproj
dotnet list src/ErpBackend/ErpBackend.csproj package --vulnerable --include-transitive

Push-Location src/erp-frontend
npm test
npm run lint
npm audit
```

## 結語

優先以一個完整業務流程（例如庫存查詢或採購收貨）練習「頁面 → API client → Controller → Service → Entity／DbContext → Migration → 測試」的追蹤方式，再擴展到其他模組。本文是靜態結構與文件導讀；建置、測試、覆蓋率及依賴安全掃描需在本機另外執行確認。
