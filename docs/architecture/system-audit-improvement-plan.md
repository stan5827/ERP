# ERP 系統現況核對與改進優化方案

**核對日期：** 2026-10-21
**核對方式：** 閱讀六份使用者附加文件，並以目前工作目錄中的原始碼、依賴設定、部署設定、CI 與測試結果交叉核對。
**重要界線：** 本文件是靜態架構與交付準備度評估，不是滲透測試、正式資料庫稽核或營運環境驗證。檢查時工作目錄已有多處未提交變更；以下描述的是當時工作目錄內容，不代表乾淨分支或正式環境狀態。

## 1. 結論摘要

本系統目前是以單一 ASP.NET Core Web API 與 PostgreSQL 為核心，搭配 React Web、Expo 員工 App、EF Core、xUnit、Docker Compose 及 Prometheus 的多應用 ERP 專案。實際程式已涵蓋人資、庫存、採購、銷售、製造/MRP/APS、財務、品質、資產、預算、研發、ESG、協作及 AI 等模組；但文件中把已實作、構想中及部署範例混寫成同一種「現況」，因此不能把所有文件描述直接視為已上線或已驗收的功能。

建議順序不是先拆微服務或大幅重寫，而是：

1. **先處理正式環境的存取控制與部署預設值。**
2. **立即更新已過期或即將停止支援的執行環境。**
3. **修正失敗的跨模組整合測試與記錄/背景工作的可靠性。**
4. **統一 API、資料庫、監控及操作文件的實際契約。**
5. **在風險下降後，以模組化單體方式逐步改善大型服務與啟動程式。**

## 2. 核對範圍與系統基線

### 2.1 已閱讀的附加文件

- `erp_system_architecture_document.md`
- `application_and_operations_portal.md`
- `business_processes_and_modules_documentation.md`
- `code_artifact.md`
- `database_documentation.md`
- `erp_api_documentation.md`

六份文件的內容偏向架構說明、使用手冊與設計提案的混合版本；部分文件標示 2026-04-06 或 v1.0，與 2026-10-21 的程式及套件現況可能已有差異。

### 2.2 目前程式結構

| 範圍 | 核對結果 |
|---|---|
| 後端 | ASP.NET Core Web API，單一 `.csproj`、目標 `net8.0`；EF Core 8 + Npgsql/PostgreSQL。 |
| 資料模型 | [ErpGeneralDbContext.cs](../../src/ErpBackend/Data/ErpGeneralDbContext.cs) 靜態列出 245 個 `DbSet`；Migration 目錄目前有 6 個 EF Core migration（另有其 Designer 與 Model Snapshot 檔案）。 |
| API | 85 個 Controller `.cs` 檔案（含 `BaseController`）；基底路由是 `/api/erp/[controller]`。目前控制器原始碼中只找到 3 處 `[Authorize]`，自訂權限標記只找到一處使用。 |
| 服務層 | 服務與介面已大量存在；目前掃描未在 Controller 原始碼中找到直接引用 `ErpGeneralDbContext`。這與舊架構文件的「73/84 個控制器直接依賴 DbContext」基線不同，應更新文件，不應繼續沿用舊數字。 |
| 大型服務 | [ProcurementService.cs](../../src/ErpBackend/Services/ProcurementService.cs) 1,137 行；`AIPlanningService.cs` 785 行；`ProcessInspectionService.cs` 545 行；`WorkOrderService.cs` 539 行。 |
| 啟動及註冊 | [Program.cs](../../src/ErpBackend/Program.cs) 約 393 行，包含大量 DI 註冊、JWT/CORS/監控設定及資料初始化。 |
| Web 前端 | React 19、TypeScript、Vite；Web app 有 54 個頁面目錄檔案。前端測試目前 1 個測試檔、2 個測試案例。 |
| 行動端 | Expo / React Native 獨立專案；其 `package.json` 沒有 test、lint 或 build 驗證指令。 |
| 測試 | 後端單元測試 Release 測試組件：625 通過、3 跳過；整合測試：233 個中 225 通過、8 失敗。 |

這些數字是檔案與測試執行結果，不代表所有業務流程已完整實作，也不等同於正式環境負載、權限或資料一致性驗證。

## 3. 優先改善項目

| 優先級 | 核對發現與證據 | 影響 | 建議 |
|---|---|---|---|
| **P0：上線前必須確認** | 控制器中只找到 3 處 `[Authorize]`；[BaseController.cs](../../src/ErpBackend/Controllers/BaseController.cs) 沒有套用授權；[Program.cs](../../src/ErpBackend/Program.cs) 未設定全域 fallback authorization policy。自訂 `[PermissionAuthorize]` 只在一個收料沖銷操作使用，且依賴明確套用 Filter。 | 靜態宣告顯示大量 API 路由沒有明確授權邊界。若這不是刻意設計，生產環境可能讓未授權使用者存取業務端點。尚未對實際執行路由逐一探測，故需以路由矩陣與整合測試完成最終確認。 | 建立全 API 路由清冊，逐一標示匿名/登入/角色/資源層級權限；預設拒絕、登入路由明確匿名例外；加入未登入、跨角色、跨公司/廠區資料隔離測試。未完成前不得宣稱 RBAC 已全面落實。 |
| **P0：正式部署阻擋** | [docker-compose.yml](../../deployment/docker-compose.yml) 含資料庫及 Grafana 的固定預設密碼，並對主機公開資料庫、Redis、Prometheus、Grafana、Alertmanager 等連接埠；Grafana 管理者憑證亦在 Compose 設定中。附件 `code_artifact.md` 另列出預設登入憑證，但 [Program.cs](../../src/ErpBackend/Program.cs) 的實際初始化流程是隨機產生初始密碼並要求首次變更。 | 文件與部署樣板可能讓示範憑證被誤認為可用正式帳密；公開管理/資料服務增加未授權存取風險。 | 移除所有可部署的固定預設憑證；強制使用秘密管理或部署注入；將管理及資料服務限制在私有網路，除必要入口外不映射主機埠；檢視既有環境是否曾使用樣板值並安排輪替。此文件不複製憑證內容。 |
| **P1：立即安排** | [Dockerfile.frontend](../../deployment/Dockerfile.frontend) 使用 Node 20；Node 官方版本表將 Node 20 標示為 EOL（最後更新為 2026-03-24）。後端仍以 .NET 8 / EF Core 8.0.11 為目標，而 Microsoft 官方支援政策列出 .NET 8 支援終止日為 **2026-11-10**。 | 前端建置映像使用已停止維護的 Node 版本；後端距 .NET 8 支援終止不到一個月，安全修補與合規窗口非常短。 | 將前端建置映像升至仍受支援的 LTS（例如 Node 24 LTS），同步驗證 Vite/Node 相容性。先套用 .NET 8 最新安全修補，再以 .NET 10 LTS 為目標安排後端、EF Core、Docker、CI 與測試的整套升級；不要只改 TargetFramework 字串。 |
| **P1：資料與流程可靠性** | [PermissionAuthorizeFilter.cs](../../src/ErpBackend/Filters/PermissionAuthorizeFilter.cs) 的 Audit Log 寫入使用未等待的 `SaveChangesAsync()`；[Program.cs](../../src/ErpBackend/Program.cs) 將 `BackgroundServiceExceptionBehavior` 設為 `Ignore`，同時註冊 4 個 Hosted Service。 | 稽核記錄可能在請求完成或 DbContext 生命週期結束前未成功落盤；背景服務發生未處理錯誤後可能停止而主站仍維持運作，造成排程工作中斷且不易察覺。 | 改為可觀測、可等待且有明確失敗處理的稽核寫入；若不能阻塞請求，使用可持久化佇列/Outbox。為每個背景工作增加存活狀態、最後成功時間、重試/冪等策略及告警；取消「忽略錯誤但不告警」的狀態。 |
| **P1：驗收及回歸** | 整合測試 8 項失敗，分布於 RCCP 資源更新、BOM 新增/更新、LLM 狀態端點、庫存結存/ATP 與銷售至財務 E2E。另 [MpsService.cs](../../src/ErpBackend/Services/MpsService.cs) 有兩處 nullable dereference 編譯警告。單元測試重建時遇到 Microsoft.CodeCoverage 對測試輸出檔案的 Access Denied；以既有 Release 測試組件 `--no-build` 執行則 625 通過、3 跳過。 | 目前無法確認主要跨模組流程全綠；測試組件重建受環境檔案權限影響，因此「通過」結果不等於完整重新建置後的測試證據。 | 先逐項分類 8 個失敗是產品回歸、測試過期或測試資料/SQLite 與 PostgreSQL 差異；修正後在乾淨 CI runner 重跑。處理兩處 nullable 警告及 coverage 檔案權限，建立可重現的測試入口。 |
| **P1：API 契約一致性** | [ApiVersionMiddleware.cs](../../src/ErpBackend/Middleware/ApiVersionMiddleware.cs) 對 `/api/vN/...` 只記錄版本並移除版本路徑，再交給其他路由；[BaseController.cs](../../src/ErpBackend/Controllers/BaseController.cs) 的 `Fail(...)` 將錯誤包裝後仍回傳 HTTP 200；[GlobalExceptionMiddleware.cs](../../src/ErpBackend/Middleware/GlobalExceptionMiddleware.cs) 則回傳不同錯誤形狀與狀態碼。 | 附件所述「所有 API 強制版本化、所有回應一致、400/401/403/500 對應一致」與實際路由及回應行為不符；客戶端可能把失敗誤判為成功。 | 以實際路由產生 OpenAPI 契約；決定並落實版本策略（拒絕未版本化路由或明確維持相容）；統一成功/錯誤 envelope 及 HTTP 狀態碼；為舊端點制定 deprecation，而非只在文件宣告。 |
| **P1：快取及執行期依賴** | Compose 啟動 Redis 並傳入連線環境變數，但後端 [CacheService.cs](../../src/ErpBackend/Services/CacheService.cs) 目前實作為 `IMemoryCache`；`Program.cs` 僅註冊 Memory Cache。正式 API 的資料庫提供者為 Npgsql，SQLite 僅見於整合測試 Factory。 | 多個後端副本間快取不一致；部署人員可能以為 Redis 已被應用程式使用；附件所稱 SQL Server/SQLite 開發支援沒有對應的 Web API provider 註冊。 | 決定是否真的需要跨副本快取：若需要才導入分散式快取並測試失效語意；若不需要則移除未使用的 Redis 服務/設定。文件清楚標示正式 provider 與測試 provider。 |
| **P1：CI 與觀測盲區** | [ci.yml](../../.github/workflows/ci.yml) 執行後端建置及 .NET 測試；未見 Web 前端 build/test 步驟。安全掃描只在 main/master push 執行。Prometheus 設定只抓取 ERP Backend 與 Prometheus；Compose 未見 PostgreSQL exporter。 | Web 前端問題可能不會被 PR 擋下；高風險變更可能等合併後才掃描；文件中資料庫連線池、主機磁碟等告警若無對應 exporter/指標，可能無法觸發。 | PR 加入 Web build/test；容器建置與依賴/映像掃描納入 PR gate。逐條比對 Alert rules 的 metric 是否由現有 scrape target 真正提供；補上 exporter 或刪除無效告警。 |
| **P2：維護性與模組邊界** | [Program.cs](../../src/ErpBackend/Program.cs) 同時負責 DI、管線與多段資料初始化；[ProcurementService.cs](../../src/ErpBackend/Services/ProcurementService.cs) 超過 1,100 行。單一 DbContext 涵蓋 245 個 `DbSet`。 | 變更影響面及測試隔離成本增加；在沒有清楚邊界前直接拆微服務會增加部署、交易一致性與維運負擔。 | 暫維持模組化單體；依業務領域拆分 DI 註冊、初始化、查詢/命令服務和交易邊界。先以依賴方向與端到端流程測試守住行為，再逐步拆分大型服務；沒有獨立部署或擴展證據前不建議先做微服務化。 |
| **P2：文件與事實來源** | [module-map.md](./module-map.md) 的服務/Controller 數字及重構狀態與目前程式不一致；附件提到 Serilog/ELK、Hangfire/Quartz、SQL Server、Line、Prisma、管理員預設登入等多項能力，但直接依賴/註冊或目前程式證據不一致。 [Migrations/Scripts/README.md](../../src/ErpBackend/Migrations/Scripts/README.md) 描述的歷史 SQL 腳本並未出現在該目錄（目前只有 README）。 | 開發、稽核、導入與維運人員可能根據提案或歷史資訊作出錯誤決策，亦可能將未實作功能當成可承諾能力。 | 將文件標成「已實作／實驗中／規劃中／已淘汰」，補上核對日期及負責人；路由與依賴版本優先由原始碼/manifest 產生；刪除失效路徑及過期 KPI，保留規劃內容但不得與現況混寫。 |

## 4. 附件與程式的關鍵差異

| 附件描述 | 程式/設定核對 | 整理原則 |
|---|---|---|
| 架構文件描述 PostgreSQL、Redis、Prometheus/Grafana、Serilog/ELK、排程器及多種 AI/LINE 整合。 | PostgreSQL、Prometheus 指標及多個 Hosted Service 有直接程式證據；Redis 目前未由後端連線；直接依賴中未找到 Serilog、ELK、Hangfire/Quartz 套件或配置；LINE 等功能須逐項以 Controller/服務與正式設定確認。 | 區分已部署/程式已接線/僅規劃，不把示意架構圖當成實際拓樸。 |
| 操作入口文件提到前端使用 React Query、Axios、React Router 與許多模組路由。 | 套件確實有 TanStack Query、Axios、React Router；但 [App.tsx](../../src/erp-frontend/src/App.tsx) 目前以 `activeModule` 狀態切換頁面，不能只以安裝套件推定目前使用 URL router。 | 文件應由實際路由宣告、側欄模組及使用者流程核對生成；標示未接線套件。 |
| 資料庫文件聲稱 PostgreSQL/SQL Server/SQLite、多幣別、稅務及所有表都具完整審計欄位。 | Web API 只看到 Npgsql provider 設定；SQLite 用於整合測試。DbContext 大型且含眾多領域模型，但「所有表都有欄位」及每個表單/流程已落地，尚未由本次逐欄位 schema 比對證實。 | 以 EF Core model snapshot、migration 及實際資料庫 schema 做欄位級驗證；未確認的欄位承諾應標為需求，不列為既有事實。 |
| API 文件聲稱所有端點強制版本化且統一回應。 | Controller 基底路徑是 `/api/erp/...`；自訂中介軟體會接受版本段並剝除，不是端點版本協商；錯誤回應實際存在不同格式與狀態碼行為。 | 以 OpenAPI 為規格來源，對前端呼叫逐條確認，再同步舊版相容政策。 |
| 操作手冊列有固定管理員示範帳密。 | 現行後端初始化是從環境變數讀取初始密碼，否則生成隨機密碼，並標記首次登入需變更。 | 從共用操作文件移除固定憑證文字；只寫部署時如何安全設定與重設。 |
| 文件以「E2E」描述銷售到財務及其他流程。 | 目前整合測試執行有 8 個失敗；前端只有 2 個 Vitest 測試案例，行動端沒有測試指令。 | 將流程規格轉成可重複執行且有持久化測試證據的跨模組驗收案例。 |

## 5. 分階段落地方案

### 第 0 階段：正式環境安全閘門

1. 建立 API endpoint × 身分/角色/資料範圍權限矩陣，涵蓋 Controller、HTTP method、匿名例外、跨公司/廠區隔離。
2. 導入全域預設拒絕與明確 `[AllowAnonymous]` 清單，逐角色測試查詢及寫入。
3. 清除部署預設帳密，檢查網路公開埠及歷史部署是否沿用樣板值，必要時輪替密碼/金鑰。
4. 將資料庫、Grafana、Prometheus、Redis 與 Alertmanager 管理介面移到內網；公開入口僅保留必要服務。
5. 將此階段設為任何正式 Go-Live 的必要簽核項目。

### 第 1 階段：支援週期、PR gate 與可重現測試

1. 前端 builder 由 Node 20 遷移至支援中的 LTS；在本機、CI 及 Docker 三處使用一致版本。
2. 後端先更新至 .NET 8 支援中的最新 servicing patch，再升級 .NET 10 LTS；同步更新 EF Core、Npgsql、SDK/runtime Docker image 及 GitHub Actions。
3. 解決測試輸出檔案權限問題，並讓乾淨 runner 可從 restore、build 到測試完整執行。
4. 為 CI PR 加入後端 build、unit tests、integration tests、前端 `npm run build` / `npm test`、依賴及容器掃描。
5. 將 8 項整合測試失敗拆解成可追蹤工作，修正後把零失敗作為合併條件。

### 第 2 階段：交易、錯誤與背景處理

1. 統一 API 成功/錯誤格式及 HTTP 狀態，補上版本相容性測試與 OpenAPI diff。
2. 將審計紀錄改為可確認完成/失敗的寫入；必要時以持久化 Outbox 承接請求端與稽核端的解耦。
3. 為 Exchange Rate Sync、Calendar Reminder、Supplier Rating、Asset Depreciation 等背景工作定義重試、冪等、排程鎖、時區、補跑及告警策略。
4. 調整 `/health` 與 readiness：分清 liveness/readiness，只有確實屬於啟動必要依賴者才納入 readiness，並測試 DB 中斷/恢復情境。
5. 以 PostgreSQL 測試 Migration、交易原子性、重複請求及關鍵帳務/庫存不可變更流程；測試用 SQLite 的差異須明確列管。

### 第 3 階段：觀測與效能基線

1. 先確認每條 Prometheus alert expression 的來源指標及 exporter；測試 alert 能否實際觸發、通知與恢復。
2. 統一結構化日誌、Trace ID/Correlation ID、PII/薪資/LLM 機密遮罩與保留政策；若正式要求集中式 logging，補上可運作且有 retention 的 sink，而不是只在架構文件提到。
3. 為核心業務流程建立負載基線（登入、庫存查詢/異動、PO 收貨、銷售結帳、MRP、Payroll）；先測量再設定 SLO，不直接沿用文件中未驗證的延遲門檻。
4. 以實際測量決定是否啟用 Redis/分散式快取、連線池指標或背景佇列；移除未使用的服務與環境變數。

### 第 4 階段：模組化單體與文件治理

1. 將 `Program.cs` 的服務註冊、Middleware、健康檢查及資料初始化拆成清楚的模組入口；把大型同步初始化移到獨立、可重跑且有稽核的種子/部署流程。
2. 先拆分 `ProcurementService` 等大型服務的責任與測試邊界；保留公開 API 和既有交易行為，不以單純行數作為唯一重構成功標準。
3. 為 Sales→Inventory→Manufacturing→Procurement→Finance、HR→Payroll 等跨域交易建立端到端契約測試及資料一致性檢查。
4. 將 `docs/architecture/module-map.md` 的控制器/服務統計改為可重算；所有文件註明「已實作、實驗中、規劃中」及資料日期。
5. 依實際 OpenAPI、EF model/migrations、CI workflow 與 Compose 設定維護 API、資料庫及部署文件；過期的示意流程與失效 SQL 路徑移除或改標歷史資料。

## 6. 驗收條件

- 所有 API 路由都有明確的匿名/已登入/角色/資料範圍規則；未授權與跨組織存取測試均符合預期。
- Compose/正式部署不含可直接使用的預設密碼；非公開管理服務不暴露在公網介面。
- 使用中的 Node、.NET、EF Core、Npgsql、容器基底映像及 CI runner 均仍在支援週期內，升級有 build/test 證據。
- 單元、整合、Web build/test、Migration 驗證與容器安全掃描在乾淨 CI runner 上通過；已知 skip 有具名技術債與負責人。
- 稽核及背景工作錯誤可被觀測、告警、重試或明確失敗；沒有無監控的 fire-and-forget 寫入或靜默停止工作。
- API 文件與實際 route、response/status、認證方式一致；schema 文件與 EF migration/正式資料庫相符。
- 業務流程通過端到端驗收並能追溯單據、庫存/帳務變動及操作者。

## 7. 本次執行的驗證

| 驗證 | 結果 |
|---|---|
| 後端單元測試（既有 Release 組件、`--no-build --no-restore`） | 625 通過、3 跳過、0 失敗。先前一般重建測試組件時，Microsoft.CodeCoverage 寫入測試輸出檔遭遇 Access Denied；因此本次未能以該指令確認最新測試原始碼完整重建。 |
| 後端整合測試 | 225 通過、8 失敗、0 跳過；失敗案例包含 RCCP、BOM、LLM、Inventory 與 Sales-to-Finance E2E。 |
| 後端建置 | 整合測試建置成功；`MpsService.cs` 兩處 nullable dereference 警告。整合測試專案另有 xUnit analyzer 與 nullable 警告。 |
| Web 前端 | `npm run build` 通過；Vitest 1 個測試檔、2 個案例通過。 |
| 行動端 | 本次未執行；專案 manifest 未提供 test/build script。 |
| 部署環境/正式資料 | 未連接或變更正式資料庫、雲端資源、容器或外部服務；沒有執行滲透測試或負載測試。 |

## 8. 參考來源

- [Microsoft .NET 支援政策](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)：.NET 8 支援終止日與受支援版本。
- [Node.js Releases](https://nodejs.org/en/about/previous-releases)：Node 20 EOL 與目前 LTS 版本狀態。
