# Implementation Plan: ERP 員工身分、登入帳號、角色與權限

**Date**: 2026-10-23 | **Spec**: [employee-identity-access-plan.md](../employee-identity-access-plan.md)

## Summary

以現有 HR `Employee`、系統 `User`、`Role`、`Permission`、`UserRole`、`RolePermission` 及 JWT/RBAC 為基礎，分階段補齊帳號生命週期、伺服器端授權、角色管理、SoD 衝突檢查與敏感人事欄位保護。不另建 HR 或認證子系統；保留現有 API 路由與回應封裝，先盤點使用端及現存資料，再以相容的 migration 和 API/UI 變更上線。

本計畫不直接假設未經業務確認的角色授權矩陣。員工留停策略、初始角色權限、SoD 衝突組合及例外核准人須在 Phase 1 取得負責人核准，作為進入資料及權限切換的放行條件。

## Technical Context

**Language/Version**: C# 12 / .NET 8; TypeScript / React 19<br>
**Primary Dependencies**: ASP.NET Core Web API, EF Core 8, BCrypt.Net-Next, JWT Bearer, Vite<br>
**Storage**: 現有 PostgreSQL / `ErpGeneralDbContext`；以 EF Core migration 漸進變更<br>
**Testing**: xUnit、現有 ASP.NET Core integration tests、前端 Vitest；Playwright 可用於必要的管理介面流程<br>
**Target Platform**: ERP Web API、React Web 前端及員工 App 所使用的既有 API<br>
**Performance Goals**: 不低於目前正式環境基線；Phase 1 先量測登入及受保護 API 延遲，Token 撤銷與即時授權檢查不得未經量測即全域部署。2026-10-24 已於隔離本機 PostgreSQL 測得成功登入 P50/P95 244.12/306.05 ms、有效 JWT API P50/P95 46.24/60.66 ms；此為合成測試數據，production 基線與延遲預算仍待核定。
**Constraints**: Controller 僅依賴 Service 介面；保留既有 API 路由／回應相容性；不刪除離職員工、歷史交易或稽核；不輸出密碼、Token 或完整敏感欄位至日誌。

**Cache/Revocation note**: `Program.cs` currently registers in-memory cache; Redis is mentioned only as optional in a comment and no Redis client/registration was found in the source scan. Do not assume Redis is an operational dependency. Use an authoritative per-user security stamp/version check for immediate revocation; consider a shared Redis cache only after verifying deployment, multi-instance consistency, invalidation behavior, and fail-closed fallback. Per-process memory cache alone is not sufficient for immediate cross-instance revocation.

## Constitution Check

未找到獨立的 `constitution.md`。以 [AGENTS.md](../../../../AGENTS.md)、[standards.md](../standards.md)、[dependency-rules.md](../dependency-rules.md) 作為本計畫的強制原則。

| 原則 | 結果 | 計畫符合方式 |
|---|---|---|
| 分層與依賴倒轉 | PASS | Controller 只呼叫服務介面，資料存取留在 Service；新服務於 `Program.cs` 註冊 |
| 非同步與型別安全 | PASS | I/O 使用 async/await，公開非同步方法以 `Async` 結尾，不以阻塞呼叫替代 |
| 既有行為及契約 | PASS | 先建立 API 使用者／契約基線；授權和欄位調整以整合測試保護現有前端與 App |
| 資料保護與稽核 | PASS | 最小授權、敏感欄位分權、停權保留歷史並記錄高風險異動 |
| 可逆遷移 | PASS | migration 前先產出資料檢查報告；分批啟用授權並預備回滾程序 |
| 身分資料安全遷移 | PASS | 唯讀 dry-run 先產出重複、孤兒及異常帳號報告；經 HR／系統管理者覆核後才修復或建立唯一約束 |
| 緊急授權可控 | PASS | Break-Glass 與一般 SoD 例外分開，雙人核准、預設最長 2 小時、自動撤銷並高優先稽核 |
| 服務／檔案大小 | PASS | 依帳號、角色授權、SoD 分責任切分；遵守單檔 500 行規範 |

## Applied Guidelines

未找到適用於本功能的 `skills/guidelines/` 技術轉換指南。實作依本專案既有 ASP.NET Core 8、EF Core、服務介面、DI、xUnit、前端 API client 與 Vitest 慣例。

## Implementation Steps

### Phase 1 — Setup：現況基線與政策確認

#### Step 1.1: 建立 API、資料與權限基線
- **Requirements**: REQ-004, REQ-005, REQ-006, REQ-007, REQ-008, REQ-011
- **Design inputs**: 規格第 2、7、8 節；`AuthController`、`EmployeeService`、`PermissionAuthorizeFilter` 及現有 API 使用者
- **Description**: 盤點全部帳號／員工／角色／權限端點與呼叫端、`[Authorize]`／`PermissionAuthorizeFilter` 覆蓋、現有權限代碼格式、JWT claims、登入／受保護 API 延遲、員工狀態值、重複及孤兒 `EmployeeId`。驗證 Redis 部署、NuGet client、DI registration、連線設定與多節點行為；程式目前只有記憶體快取註冊和 Redis optional 註解，故在證明可用前不依賴 Redis 執行授權。輸出基線與差異清單，不先變更現有資料。

#### Step 1.2: 核定角色、狀態與 SoD 政策
- **Requirements**: REQ-004, REQ-006, REQ-009, REQ-010
- **Design inputs**: 規格第 3.1、3.3–3.5、10 節
- **Description**: 由 HR、財務、採購、倉儲、生產、品保、業務及系統負責人以正式簽核表或有出席／決議紀錄的會議逐項核定 `module:resource:action`、角色、`active`／`leave`／`terminated` 登入政策、SoD 衝突、Break-Glass 核准責任與期限、資料範圍及薪資欄位。矩陣須有模組負責人、核准人、版本、生效日及變更紀錄，存入 `docs/architecture/employee-identity-access/authorization-matrix.md`。Phase 2 開發及授權 migration 的放行門檻為所有受影響模組正式核定；未核定權限不得以全域管理員權限代替。

### Phase 2 — Foundational：相容性、資料與授權基礎

#### Step 2.1: 完成既有身份資料唯讀預檢與核准修復
- **Requirements**: REQ-001, REQ-003, REQ-005
- **Design inputs**: 規格第 3.1、6、7 節；`User.EmployeeId` 與既有唯一索引
- **Description**: 先執行 dry-run，產出重複 `EmployeeId`、無效／孤兒員工帳號、孤兒角色／權限及將受唯一約束影響的清單與筆數；報告不修改資料且不包含密碼或敏感欄位。由 HR 與系統管理者逐筆決定修復、保留或例外並留下簽核；修復採可重跑且有前後筆數驗證的腳本。未核准資料不得新增 `EmployeeId` 唯一限制；不得自行合併、刪除或停用無法判定的帳號。

#### Step 2.2: 定義 Token 版本檢查與伺服器授權策略
- **Requirements**: REQ-004, REQ-006, REQ-011
- **Design inputs**: 規格第 3.1、3.3 節；現有 JWT 8 小時設定與資料庫權限查詢
- **Description**: 在 `User` 設定安全版本／security stamp，登入時將其版本寫入 JWT；帳號停用、密碼重設、員工離職／留停、角色指派或權限變更時更新版本。每次受保護請求比對權威版本並確認員工與帳號狀態，確保舊 Token 立即失效。以 PostgreSQL/既有安全狀態作為授權真實來源；只有在 Step 1.1 證明 Redis 已部署且具共享一致性後，才可將 Redis 作為效能快取，並驗證失效發布／讀取及 Redis 失效時回查權威來源或拒絕請求。不得以本機 memory cache 降低即時撤銷保證。

#### Step 2.3: 建立新增資料／授權範圍的 migration 與測試基礎
- **Requirements**: REQ-001, REQ-004, REQ-007, REQ-009
- **Design inputs**: 規格第 6、8 節；現有 `SystemModels.cs`、`ErpGeneralDbContext.cs`
- **Description**: 根據已核定設計，新增必要的帳號鎖定／安全版本、角色代碼、SoD 規則、一般例外及 Break-Glass grant 欄位或實體；設定外鍵、索引、唯一限制及 EF migration。資料範圍先支援本人、單一部門與部門子樹、全公司；使用現有 `Department.ParentDeptId` 解析核准的部門階層，不建立通用動態查詢過濾器。只新增確實需要的欄位，不重複建立現有實體；加入 migration 模型／DI 的單元或整合測試。

### Phase 3 — US1 (P1)：安全帳號與員工生命週期

#### Step 3.1: 封閉自助註冊並實作受控帳號開通
- **Requirements**: REQ-001, REQ-002, REQ-007, REQ-012, REQ-014
- **Design inputs**: 規格第 3.1、4 情境 A；`AuthController`、`AuthService`、`UserService`
- **Description**: 將內部帳號建立限定為經授權管理流程；驗證員工有效、帳號名稱唯一、員工未有帳號、角色存在且指派者有權限。移除匿名註冊造成的內部帳號建立途徑，保留既有 route 時以明確拒絕或相容轉介處理，不回傳認證秘密。

#### Step 3.2: 實作帳號鎖定、重設及認證稽核
- **Requirements**: REQ-002, REQ-007, REQ-012, REQ-014
- **Design inputs**: 規格第 3.1、8 節；`AuthService` 既有 BCrypt 與密碼歷史
- **Description**: 失敗登入依帳號累計，預設連續 5 次錯誤鎖定 15 分鐘；成功登入或管理員手動解除鎖定會清除失敗計數，鎖定逾時自動解除。此門檻與時間可由 `Auth:Lockout` 設定覆寫。管理員重設密碼須伺服器端授權、強制下次改密碼、遵守密碼歷史並記錄操作者；移除預設秘密 fallback，只接受明確設定的管理重設秘密。

#### Step 3.3: 同步員工狀態、帳號狀態及 Token 撤銷
- **Requirements**: REQ-003, REQ-004, REQ-007, REQ-010, REQ-011
- **Design inputs**: 規格第 3.1、4 情境 B；`EmployeeService.TerminateAsync`、`RehireAsync`、登入與 JWT 驗證
- **Description**: 於同一資料庫交易中更新離職／停留狀態與登入帳號有效狀態；依 Step 2.2 使既有 Token 及權限即時失效。復職只恢復人事狀態，不默默恢復帳號或舊角色。部門／職稱／主管異動產生權限審查清單，避免未核准自動擴權。

#### Step 3.4: 加入員工與帳號生命週期測試
- **Requirements**: REQ-001, REQ-002, REQ-003, REQ-004, REQ-007, REQ-010, REQ-011
- **Design inputs**: 規格第 4 情境 A、B 與成功標準 SC-001、SC-002、SC-007
- **Description**: 新增服務與整合測試，覆蓋重複帳號、無效員工、匿名註冊拒絕、離職後舊 Token、留停政策、復職不自動恢復角色、資料庫交易回滾及既有登入回應契約。

### Phase 4 — US2 (P1)：權限目錄、角色指派與 SoD

#### Step 4.1: 建立一致權限目錄與相容遷移
- **Requirements**: REQ-005, REQ-006, REQ-011
- **Design inputs**: 規格第 3.3、7 Phase 2；`Permission`、登入 JWT claims、`PermissionAuthorizeFilter`
- **Description**: 依核定權限矩陣建立唯一 `module:resource:action` 目錄及舊代碼映射。更新登入授權計算、伺服器端授權比對與前端使用資料；先遷移並比對舊碼，不在執行期同時接受模糊或衝突格式。

#### Step 4.2: 實作角色及權限管理服務與 API
- **Requirements**: REQ-005, REQ-006, REQ-007, REQ-011
- **Design inputs**: 規格第 3.3、3.4 節；目前 stub `RolesController`、`PermissionsController`
- **Description**: 新增小型服務介面及實作，提供角色／權限清單、角色建立與更新、角色權限設定及帳號角色指派。所有管理端點須要求相應權限、驗證輸入、以交易儲存並記錄變更前後差異；保護系統內建角色及內建權限不被刪除。

#### Step 4.3: 實作 SoD 衝突檢查、核准例外與期限撤銷
- **Requirements**: REQ-007, REQ-009, REQ-013
- **Design inputs**: 規格第 3.5、4 情境 C
- **Description**: 指派角色或更新角色權限時，對每位受影響帳號計算角色合併後的有效權限；拒絕與核定矩陣衝突的組合，或要求具名核准、理由及到期時間。一般 SoD 例外與 Break-Glass 使用不同授權類型及稽核事件；一般核准流程檢查申請人／單據建立者不得自我核准。

#### Step 4.6: 建立限時 Break-Glass 緊急授權流程
- **Requirements**: REQ-007, REQ-009, REQ-013
- **Design inputs**: 規格第 3.5、情境 C、REQ-013；核定的緊急管理政策
- **Description**: 提供獨立的緊急權限申請、不同人核准、最小必要權限、事件編號／理由、明確到期時間及自動撤銷；預設最長 2 小時且不得超過核定上限。記錄申請、核准、每次高權限使用、撤銷與到期事件並觸發高優先通知。無法取得第二位核准人時，不得自我核准；僅可進入預先核定的雙人／事後覆核程序，需有短期限及稽核，且不得留下永久權限。

#### Step 4.4: 對受保護 API 落實伺服器端授權
- **Requirements**: REQ-004, REQ-006, REQ-009, REQ-011, REQ-015, REQ-016
- **Design inputs**: 規格第 3.3、8 節及 Phase 1 API 基線
- **Description**: 依 API 權限矩陣逐模組標註並強制執行權限；移除「未加權限屬性即放行」的保護端點行為，對不需登入的端點明確列入 allow-list。授權服務使用現行角色／權限資料；撤銷或員工停用後拒絕舊 Token。維持 401 未登入、403 無權限與既有錯誤包裝一致。

#### Step 4.5: 驗證角色管理、SoD 與授權負向情境
- **Requirements**: REQ-005, REQ-006, REQ-007, REQ-009, REQ-011
- **Design inputs**: 規格第 4 情境 C 與成功標準 SC-003、SC-004、SC-006
- **Description**: 新增服務及 API 整合測試，驗證多角色聯集、無權限／未登入、角色被撤銷、系統角色保護、跨角色 SoD 衝突、例外核准／到期、單據自我核准阻擋及 API 權限覆蓋盤點。

### Phase 5 — US3 (P1)：敏感員工資料與管理介面

#### Step 5.1: 分離員工一般資料與敏感資料回應
- **Requirements**: REQ-007, REQ-008, REQ-012
- **Design inputs**: 規格第 3.2、4 情境 D；`EmployeeDetailDto`、`EmployeeService`、`EmployeesController`
- **Description**: 依核定資料分級調整 DTO 與查詢：一般清單不含個資／薪酬；身分證、銀行、薪資、投保及緊急聯絡資訊使用明確欄位權限與資料範圍。先用服務層明確條件支援本人、單一部門及直屬／核定下屬部門子樹；子樹依既有 `Department.ParentDeptId` 遞迴解析，再把允許的 DepartmentId 集合套用到員工查詢。暫不抽象成跨系統通用動態 query filter。保留既有路由及必要欄位相容性，但未授權值不得回傳；稽核只記錄欄位類別，不寫入實際值。

#### Step 5.2: 保護個人資料匯出、稽核及清單查詢
- **Requirements**: REQ-007, REQ-008, REQ-012
- **Design inputs**: 規格第 3.2、8 節；既有 `AuditLog` 與 HR 查詢／匯出路徑
- **Description**: 對薪酬、銀行與身分資料的讀取／匯出執行授權與資料範圍檢查；依核定規則遮罩列表資訊。檢查應用程式日誌、API 錯誤及稽核內容不包含密碼雜湊、JWT 或完整敏感資料。

#### Step 5.3: 更新 Web 權限上下文及系統管理頁
- **Requirements**: REQ-002, REQ-005, REQ-006, REQ-008, REQ-009, REQ-011
- **Design inputs**: 規格第 3.3–3.5、4 節；`RoleContext.tsx`、`ProtectedRoute.tsx`、`src/api/auth.ts`、`src/api/system.ts`、`SystemSettings.tsx`、`HR.tsx`
- **Description**: 以登入使用者的有效權限資料驅動 UI，避免只取 `roles[0]` 或自行猜測權限；新增帳號開通／停用、角色指派、權限矩陣及 SoD 衝突提示。無權限頁面不呈現操作入口，API 仍獨立拒絕；401／403 與登入到期提供明確提示。

#### Step 5.4: 驗證欄位級保護與管理者主要流程
- **Requirements**: REQ-007, REQ-008, REQ-009, REQ-011, REQ-012, REQ-013
- **Design inputs**: 規格第 4 情境 A、C、D 及成功標準 SC-004、SC-005、SC-006、SC-007、SC-008、SC-009
- **Description**: 加入前端測試及必要的瀏覽器流程測試；以低權限及授權 HR／薪資帳號分別驗證員工清單、明細、部門階層範圍、角色管理、SoD／Break-Glass 和匯出。確認 JSON、錯誤訊息與畫面不洩漏完整敏感資料；驗證緊急權限在上限內自動失效且所有使用事件可追溯。

### Phase 6 — Polish：全域回歸、部署與回滾

#### Step 6.1: 完成端對端安全與契約回歸
- **Requirements**: REQ-001–REQ-013
- **Design inputs**: 全部驗收情境、成功標準 SC-001–SC-009
- **Description**: 跑後端 build、單元測試、架構測試與相關整合測試；跑前端 build、Vitest 及管理流程測試。檢查所有受保護 API 權限對照與 401／403、Token 跨節點即時撤銷、部門子樹資料範圍、Break-Glass 核准／到期及資料 dry-run／修復報告；與基線比較登入及 API 延遲，確認沒有超出已核准 SLA。

#### Step 6.2: 準備分批 rollout、監控與回滾
- **Requirements**: REQ-003, REQ-004, REQ-006, REQ-007, REQ-011
- **Design inputs**: 規格第 7 Phase 3 及相容性／可回滾要求
- **Description**: 編寫資料預檢、migration、角色／權限匯入、API 授權啟用及回滾順序；先以報告模式或小範圍角色驗證，再逐模組強制。定義登入拒絕、401／403 異常及 DB 授權查詢延遲的告警與停止條件；任何未知角色或資料異常先停在人工覆核，不全權放行。

## Task Breakdown

### Phase 1 — Setup

- [x] T001 [Plan:1.1] 盤點 `/api/erp` 員工、登入、帳號、角色及權限端點、前端／App 呼叫端、授權標註與 JWT claims，將現況和缺口記錄至 `docs/architecture/employee-identity-access/baseline.md`。
- [x] T002 [P] [Plan:1.1] 建立唯讀資料預檢查詢 `tools/scripts/employee-identity-preflight.sql`，涵蓋重複 `User.EmployeeId`、無效員工關聯、孤兒角色／權限及既有員工狀態分布；不輸出敏感欄位值。
- [ ] T003 [Plan:1.1] 擷取登入與代表性受保護 API 延遲基線及目前成功／拒絕結果，記錄於 `docs/architecture/employee-identity-access/baseline.md`。
- [ ] T004 [Plan:1.2] 由 HR、財務、採購、倉儲、生產、品保、業務及系統負責人以正式簽核表或具名會議決議核定角色矩陣、`module:resource:action`、留停政策、SoD、Break-Glass 責任與期限、部門範圍及敏感欄位；在 `docs/architecture/employee-identity-access/authorization-matrix.md` 留存版本、生效日、核准人及變更責任人，未簽核模組不得進入 Phase 2 權限 migration。
- [ ] T005 [Plan:2.2] 根據基線選定 Token／即時權限撤銷策略和效能預算，記錄於 `docs/architecture/employee-identity-access/authorization-decisions.md`；核實 Redis client、部署與共享一致性。預設採權威 security stamp/version 檢查；Redis 只有通過可用性與失效一致性驗證才作共享快取，未決方案不得進入全域 API 強制授權。

### Phase 2 — Foundational

- [ ] T006 [Plan:2.1] 執行唯讀 `tools/scripts/employee-identity-preflight.sql` dry-run，產出重複／孤兒帳號明細、影響筆數及預期唯一約束衝突至 `docs/architecture/employee-identity-access/data-remediation.md`；確認報告不修改資料且不含敏感值，逐筆由 HR 與系統管理者核准後才執行可重跑修復。（2026-10-23 已唯讀執行並記錄彙總；1 筆啟用帳號未關聯員工，待具名覆核，故工作未結案。）
- [ ] T007 [Plan:2.2] 新增授權服務介面及測試基架於 `src/ErpBackend/Services/IAccessControlService.cs`、`src/ErpBackend/Services/AccessControlService.cs`、`tests/ErpBackend.Tests/Services/AccessControlServiceTests.cs`，集中處理帳號有效性、目前角色／權限及權威 security stamp/version 比對；不可使用單一服務節點的 memory cache 作即時撤權來源。
- [ ] T008 [Plan:2.3] 依核定方案更新 `src/ErpBackend/Models/System/SystemModels.cs`、`src/ErpBackend/Data/ErpGeneralDbContext.cs`，建立必要的 EF migration（輸出至 `src/ErpBackend/Migrations/`），為員工帳號唯一性、鎖定／撤銷及 SoD 例外加入外鍵、索引及資料庫限制。
- [ ] T009 [Plan:2.3] 在 `src/ErpBackend/Program.cs` 註冊新服務並補充 `tests/ErpBackend.Tests/Architecture/` 相關測試，驗證 DI、服務介面及分層依賴。

### Phase 3 — US1：帳號與員工生命週期

- [x] T010 [US1] [Plan:3.1] 更新 `src/ErpBackend/Controllers/AuthController.cs` 與 `src/ErpBackend/Services/AuthService.cs`，關閉未授權的內部註冊並保留既有 route 的明確相容回應；新增受控開通驗證。
- [x] T011 [US1] [Plan:3.1] 更新 `src/ErpBackend/Services/UserService.cs` 及 `src/ErpBackend/Controllers/UsersController.cs`，要求有效員工、檢查唯一帳號與角色授權，支援停用但不刪除歷史；`CreateUserRequest` 已具備員工與角色欄位，故不需改動 DTO。
- [x] T012 [US1] [Plan:3.2] 更新 `src/ErpBackend/Services/AuthService.cs`、`src/ErpBackend/Controllers/AuthController.cs` 及 `src/ErpBackend/Controllers/UsersController.cs`，落實帳號失敗登入鎖定、管理員手動解鎖、密碼重設授權／強制改密碼／稽核及移除硬編碼重設秘密 fallback。
- [ ] T013 [US1] [Plan:3.3] 更新 `src/ErpBackend/Services/EmployeeService.cs` 及 `src/ErpBackend/Services/AccessControlService.cs`，讓離職／留停狀態與登入帳號停權同一交易提交；復職不自動恢復帳號及角色。
- [ ] T014 [US1] [Plan:3.3] 更新 `src/ErpBackend/Program.cs` 的 JWT 驗證設定及 `src/ErpBackend/Services/AccessControlService.cs`，依核定方案於受保護請求檢查帳號、員工狀態和目前權限；服務不可用時拒絕受保護操作。
- [ ] T015 [US1] [Plan:3.3] 更新 `src/ErpBackend/Services/EmployeeService.cs` 與人事異動 DTO／測試，將部門、職稱、主管變更列入角色審查，不自動增加權限。
- [ ] T016 [US1] [Plan:3.4] 新增 `tests/ErpBackend.Tests/Services/EmployeeAccountLifecycleTests.cs` 與 `tests/ErpBackend.IntegrationTests/EmployeeAccountLifecycleIntegrationTests.cs`，覆蓋重複帳號、開通拒絕、離職後舊 Token、留停、復職及交易回滾。

### Phase 4 — US2：RBAC、管理 API 與 SoD

- [ ] T017 [US2] [Plan:4.1] 建立舊權限碼對照及新目錄種子／migration，將 `src/ErpBackend/Models/System/SystemModels.cs`、`src/ErpBackend/Data/ErpGeneralDbContext.cs` 和登入權限產生統一至 `module:resource:action`。
- [ ] T018 [US2] [Plan:4.2] 建立 `src/ErpBackend/Services/IRoleService.cs`、`src/ErpBackend/Services/RoleService.cs`，實作角色查詢、建立／更新、權限設定、內建角色保護、帳號角色指派及變更稽核。
- [ ] T019 [US2] [Plan:4.2] 建立 `src/ErpBackend/Services/IPermissionService.cs`、`src/ErpBackend/Services/PermissionService.cs`，實作權限目錄查詢、模組／資源／動作驗證與啟用狀態控管。
- [ ] T020 [US2] [Plan:4.2] 將 `src/ErpBackend/Controllers/RolesController.cs` 與 `src/ErpBackend/Controllers/PermissionsController.cs` 的 stub 改為受控 API，新增 DTO 到 `src/ErpBackend/DTOs/SystemDtos.cs`，並於 `src/ErpBackend/Program.cs` 註冊服務。
- [ ] T021 [US2] [Plan:4.3] 新增 `src/ErpBackend/Services/ISodService.cs`、`src/ErpBackend/Services/SodService.cs` 及 SoD 例外 DTO，實作跨角色有效權限衝突、具名核准、理由、期限、到期撤銷及單據自我核准阻擋。
- [ ] T022 [US2] [Plan:4.4] 更新 `src/ErpBackend/Filters/PermissionAuthorizeFilter.cs` 及 `src/ErpBackend/Controllers/` 權限標註，使權限比對一致且受保護端點預設拒絕；明確測試並列出匿名 allow-list。
- [ ] T023 [US2] [Plan:4.5] 新增 `tests/ErpBackend.Tests/Services/RoleServiceTests.cs`、`tests/ErpBackend.Tests/Services/SodServiceTests.cs` 及 `tests/ErpBackend.IntegrationTests/RolePermissionIntegrationTests.cs`，覆蓋多角色、403、系統角色保護、衝突、例外到期及舊 Token 撤權。
- [ ] T024 [US2] [Plan:4.5] 完成 API 權限覆蓋測試及清冊 `tests/ErpBackend.IntegrationTests/AuthorizationCoverageTests.cs`，確認所有列為受保護的 API 均在無權限請求時回 401／403，且不改變既有 route。
- [ ] T025 [US2] [Plan:4.6] 建立 `src/ErpBackend/Services/IBreakGlassService.cs`、`src/ErpBackend/Services/BreakGlassService.cs` 及 `src/ErpBackend/Controllers/BreakGlassController.cs`，實作具名申請、不同人核准、最小權限、預設最長 2 小時、即時生效與到期撤銷、高優先稽核／通知；拒絕自我核准並支援核定的雙人或事後覆核。

### Phase 5 — US3：敏感資料與管理介面

- [ ] T026 [US3] [Plan:5.1,5.4] 於 `src/ErpBackend/Services/EmployeeService.cs` 和 `tests/ErpBackend.Tests/Services/EmployeeDataScopeTests.cs` 實作本人、單一部門及核定下屬部門子樹範圍；以 `Department.ParentDeptId` 取得可見部門集合並驗證跨部門員工不可由列表、明細或匯出取得。
- [ ] T027 [US3] [Plan:5.1] 更新 `src/ErpBackend/DTOs/EmployeeDtos.cs`、`src/ErpBackend/Services/EmployeeService.cs` 及 `src/ErpBackend/Controllers/EmployeesController.cs`，依欄位權限和資料範圍保護身分、薪酬、銀行、投保及緊急聯絡資料，清單不回傳敏感欄位。
- [ ] T028 [US3] [Plan:5.2] 更新 `src/ErpBackend/Filters/` 與相關匯出服務，對敏感欄位查詢／匯出授權並稽核；加入 `tests/ErpBackend.Tests/Services/EmployeeSensitiveDataTests.cs` 驗證日誌及稽核不含敏感明文。
- [ ] T029 [US3] [Plan:5.3] 更新 `src/erp-frontend/src/contexts/RoleContext.tsx`、`src/erp-frontend/src/components/ProtectedRoute.tsx`、`src/erp-frontend/src/api/auth.ts` 及 `src/erp-frontend/src/api/system.ts`，以伺服器回傳的有效權限更新 UI 和登入過期狀態。
- [ ] T030 [US3] [Plan:5.3] 在 `src/erp-frontend/src/pages/SystemSettings.tsx` 加入帳號、角色、權限矩陣及 SoD 管理流程，並於 `src/erp-frontend/src/pages/HR.tsx` 導入敏感欄位權限提示；沿用既有 API client 和元件慣例。
- [ ] T031 [US3] [Plan:5.4] 新增 `src/erp-frontend/src/contexts/RoleContext.test.tsx`、`src/erp-frontend/src/pages/SystemSettings.test.tsx` 及必要的瀏覽器流程，驗證無權限 UI、管理核派、SoD 衝突與敏感欄位顯示。
- [ ] T032 [US3] [Plan:5.4] 新增 `tests/ErpBackend.IntegrationTests/EmployeeSensitiveDataIntegrationTests.cs`，以一般員工、HR 與薪資授權角色驗證列表、明細、匯出及稽核結果。

### Phase 6 — Polish 與 rollout

- [ ] T033 [Plan:6.1] 執行並修正 `dotnet build src/ErpBackend/ErpBackend.csproj`、`dotnet test tests/ErpBackend.Tests/ErpBackend.Tests.csproj`、相關整合測試、`npm run build` 及 `npm test`；記錄結果與基線差異於 `docs/architecture/employee-identity-access/validation.md`。
- [ ] T034 [Plan:6.1] 核對 `docs/architecture/employee-identity-access/authorization-matrix.md` 對應所有保護端點、匿名 allow-list、角色權限、SoD／Break-Glass 衝突及敏感資料欄位；未對應或未測試的端點不得放行。
- [ ] T035 [Plan:6.2] 撰寫 `docs/architecture/employee-identity-access/rollout-and-rollback.md`，列明資料修復核准、migration 順序、權限報告／小範圍試行、逐模組啟用、停機條件、監控及回滾。
- [ ] T036 [Plan:6.2] 以隔離測試資料驗證 migration 可前進／回滾、角色權限種子可重跑、既有帳號不被意外停權，並完成正式環境放行核對清單。

## Project Structure

```text
docs/architecture/employee-identity-access/
  plan.md
  authorization-matrix.md                 # Phase 1 核定產物
  authorization-decisions.md              # Phase 1 技術／政策決議
  baseline.md                             # Phase 1 基線
  data-remediation.md                     # Phase 2 資料處理覆核清單
  validation.md                           # Phase 6 驗證證據
  rollout-and-rollback.md                 # Phase 6 上線／回滾手冊
  checkpoints/
    spec-to-plan.yaml
    plan-to-tasks.yaml
src/ErpBackend/
  Controllers/{Auth,Employees,Users,Roles,Permissions}Controller.cs
  DTOs/{Employee,System}Dtos.cs
  Filters/PermissionAuthorizeFilter.cs
  Models/System/SystemModels.cs
  Services/{AccessControl,Auth,Employee,User,Role,Permission,Sod}Service.cs
    Services/BreakGlassService.cs
  Data/ErpGeneralDbContext.cs
  Migrations/
tests/
  ErpBackend.Tests/Services/
  ErpBackend.IntegrationTests/
src/erp-frontend/src/
  contexts/RoleContext.tsx
  components/ProtectedRoute.tsx
  api/{auth,system}.ts
  pages/{HR,SystemSettings}.tsx
tools/scripts/
  employee-identity-preflight.sql
```

## Testing Strategy

- **appType**: Mixed — ASP.NET Core JSON API、React SPA、既有員工 App API consumer
- **Critical user journeys**:
  1. 管理者為有效員工開通帳號並指派角色；重複／不合格員工請求被拒絕。
  2. 員工離職／留停後，登入及舊 Token 立即無法存取；復職不會自動恢復舊授權。
  3. 管理者修改角色／權限；新衝突被阻止或有期限核准，撤權對既有 Token 生效。
  4. 緊急管理者透過不同人核准取得最小 Break-Glass 權限，並在 2 小時內自動失效及留下稽核。
  5. 主管查詢只能看到直屬及核定下屬部門員工；一般角色無法讀取薪資、銀行及身份識別資料。
  6. 前端呈現與 API 授權一致，既有登入、HR 與員工 App 端點契約維持相容。
- **primaryValidationStack**: `dotnet build`、xUnit unit/integration tests、`npm run build`、Vitest；必要時使用既有 Playwright 配置驗證主要 UI 流程。
- **fallbackMatrix**:
  - `infra-tier`: PostgreSQL 整合測試環境 → 只對服務邏輯使用 EF InMemory；migration、唯一限制及 PostgreSQL 交易行為不得以 InMemory 結果取代。
  - `browser-tier`: Playwright 真實瀏覽器流程 → Vitest 元件／API client 測試；啟用 fallback 時不得宣稱端到端 UI 已驗證。
- **Environment requirements**: .NET 8 SDK、Node/npm；整合測試需 PostgreSQL 測試資料庫；瀏覽器測試需 Playwright browser runtime。
- **knownGaps**: InMemory 無法驗證 PostgreSQL unique index、migration 與隔離交易；未跑 Playwright 時只可聲明元件與 API 層驗證；員工 App 需納入契約回歸但不在本次 UI 開發範圍。
- **Test data strategy**: 使用唯一測試員工編號及 GUID、專用角色／權限；測試成功與失敗路徑均清理帳號與關聯，絕不使用正式員工個資。
- **Acceptance criteria**: 所有新舊必要測試通過；員工狀態／授權撤銷拒絕有效、未授權敏感資料無法從 API 或匯出取得、主管部門範圍正確、SoD 衝突被阻止或有核准期限、Break-Glass 最長 2 小時後自動撤銷、既有 route／契約回歸通過、效能符合核定基線。
- **Validation review expectations**: 審查者確認 16 項 REQ 全追溯、權限覆蓋無漏端點、migration 可回滾、dry-run 資料修復有逐筆核准、Break-Glass 到期及稽核證據完整、日誌無秘密／個資外洩、CI build/unit/integration/architecture/frontend gates 通過。

## Requirement Mapping

| REQ ID | Description | Plan Items | Implementation Evidence |
|---|---|---|---|
| REQ-001 | 員工為內部身分來源且一員工至多一帳號 | 1.1, 2.1, 2.3, 3.1, 3.4 | `User.EmployeeId` constraint, `UserService`, lifecycle tests |
| REQ-002 | 僅授權管理者開通／停用／啟用帳號 | 1.1, 3.1, 3.2, 5.3 | `AuthController`, `UsersController`, `UserService`, admin UI |
| REQ-003 | 離職保留員工、帳號、交易與稽核歷史 | 2.1, 3.3, 3.4, 6.2 | lifecycle transaction, migration preflight, integration tests |
| REQ-004 | 員工狀態、帳號與既有 Token 狀態一致 | 1.1, 1.2, 2.2, 3.3, 4.4 | JWT validation, `AccessControlService`, revoked-token tests |
| REQ-005 | 多角色、多權限及權限來源可查 | 1.1, 2.1, 4.1, 4.2, 4.5 | `RoleService`, `PermissionService`, role APIs/tests |
| REQ-006 | 統一權限代碼並於伺服器端執行 | 1.1, 1.2, 2.2, 4.1, 4.4, 4.5 | permission catalog, authorization filter, endpoint coverage tests |
| REQ-007 | 帳號、授權、例外及敏感讀取具稽核 | 1.1, 2.3, 3.1, 3.2, 3.3, 4.2, 4.3, 5.1, 5.2 | `AuditLog` events, service tests and integration evidence |
| REQ-008 | 員工敏感資料最小回傳、遮罩、欄位及部門階層範圍權限 | 1.2, 2.3, 5.1, 5.2, 5.4 | employee DTO mappings, department subtree scope service/tests |
| REQ-009 | 跨角色 SoD 衝突、限期例外與緊急授權控制 | 1.2, 2.3, 4.3, 4.5, 4.6, 5.3 | `SodService`, exception and emergency grant audit/expiry tests |
| REQ-010 | 組織異動觸發角色審查 | 1.2, 3.3, 3.4 | employee change review, lifecycle integration tests |
| REQ-011 | API 相容且未登入／無權限回應明確 | 1.1, 2.2, 3.3, 3.4, 4.2, 4.4, 4.5, 5.3, 5.4, 6.1 | API contract and 401/403 integration tests, UI handling |
| REQ-012 | 日誌不得包含密碼雜湊、Token 或完整個資 | 3.2, 5.1, 5.2, 5.4 | auth/audit redaction and sensitive-data leakage tests |
| REQ-013 | Break-Glass 需分人核准、最小權限、最長 2 小時及自動撤銷 | 1.2, 2.3, 4.3, 4.6, 5.4, 6.1 | `BreakGlassService`, `BreakGlassController`, approval/expiry/audit tests |
| REQ-014 | 帳號、角色及權限管理 API 僅接受伺服器端授權；匿名帳號建立／重設一律拒絕 | 3.1, 3.2, 4.2, 4.4 | `AuthController`, `UsersController`, `SystemSettingsController`, 401/403 integration tests |
| REQ-015 | 權限節點不自動授予資料存取權，受保護 API 缺少政策時拒絕 | 4.1, 4.4, 6.1 | permission-to-endpoint policy mapping, explicit anonymous allow-list, authorization coverage tests |
| REQ-016 | 角色、權限或帳號停用後，舊 JWT 依核定時限失效 | 2.2, 3.3, 4.4, 6.1 | security stamp/version validation and revoked-token integration tests |
