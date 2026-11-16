# Phase 0 — 權限盤點與基線確認

## 執行狀態

本文件是程式碼與本機開發環境的初步基線，不代表正式環境設定、資料庫資料或業務政策已完成核定。

| 工作 | 狀態 | 證據／限制 |
|---|---|---|
| T001 API、前端／App 與授權標註盤點 | 已完成（靜態初盤） | 本文件；屬原始碼掃描，需在授權覆蓋測試建立後再以實際 endpoint 清冊複核 |
| T002 唯讀資料預檢 SQL | 已完成（腳本已備妥並執行） | [employee-identity-preflight.sql](../../../tools/scripts/employee-identity-preflight.sql)；本次唯讀執行摘要見 [data-remediation.md](./data-remediation.md) |
| T003 延遲與成功／拒絕基線 | 部分完成，等待測試環境 | 已測健康檢查、合成無效登入及未登入受保護端點；2026-10-23 10:29（+08:00）複查未找到測試帳號／Token 設定，未測成功登入／授權請求 |
| T004 權限與政策簽核 | 五類角色邊界已由使用者核定；正式模組權限簽核待辦 | [authorization-matrix.md](./authorization-matrix.md) 已記錄核定角色基準，仍須由各模組權責主管完成正式簽核 |
| T005 Token 撤銷決策與效能預算 | 暫定技術建議，待核定 | [authorization-decisions.md](./authorization-decisions.md)；缺成功請求效能樣本，尚未設定 SLA 預算 |
| T006 身份資料唯讀預檢 | 查詢完成，等待逐筆覆核 | [data-remediation.md](./data-remediation.md)；發現 1 筆啟用帳號未關聯員工，未修改資料 |

## 盤點方式與範圍

- 靜態檢視 `src/ErpBackend/Controllers/`、`Program.cs`、JWT／授權過濾器、身份模型及 EF Core 索引。
- 檢視 Web 與員工 App 的相關 API client，並確認本機開發 API、Web 與 PostgreSQL port 的可連線狀態。
- Endpoint 統計來自原始碼屬性掃描，不是 ASP.NET Core 執行期 endpoint metadata 清冊。Controller 繼承路由、路由別名、版本化及動態映射可能影響實際 endpoint 數，後續須以執行期清冊／測試複核。

## API 與授權現況

- 靜態掃描發現 89 個 Controller 原始碼檔、384 個 HTTP method attribute，以及 40 個明確宣告 class-level `Route` 的 Controller。這些數字是屬性／檔案統計，不應直接解讀為正式 API 數。
- 掃描發現 21 個 `[Authorize]` 標註、0 個 `[AllowAnonymous]` 標註，以及 1 個 `PermissionAuthorizeFilter` 套用點，分布於 9 個 Controller 檔案。`Program.cs` 有 `UseAuthentication()` 與 `UseAuthorization()`，但掃描未找到 fallback policy 或全域授權要求；因此沒有明確標註的端點不會因呼叫 `UseAuthorization()` 而自動要求登入。
- 本次人工抽查的身份／授權相關路徑：

| Controller／路由 | 觀察到的 API | 初步授權狀態 |
|---|---|---|
| `api/erp/auth` | `login`、`register`、`change-password`、`admin-reset` | 只有 `change-password` 有 `[Authorize]`；其餘路徑未見授權標註 |
| `api/erp/users` | 列表、明細、建立、停用 | 抽查 action 未見授權標註 |
| `api/erp/system-settings` | 權限樹／維護、帳號及角色查詢／維護、主題 | 抽查 action 未見授權標註 |
| `api/erp/employees` | 列表、明細、建立、更新、離職、復職 | 抽查 action 未見授權標註 |
| `api/erp/roles`、`api/erp/permissions` | `ping` | 目前為 stub；未見管理 API 實作 |
| `api/erp/finance/accounting-mappings` | 查詢 | 有 `[Authorize]`；作為本次未登入拒絕延遲測試路徑 |
| `api/erp/goodsreceipts` | 特定 action | 同時有 `[Authorize]` 與 `PermissionAuthorize` 標註，是唯一找到的自訂權限 filter 使用點 |

以上是基線缺口，不是本次變更或完整安全審查；不得將未標註一概推論為所有部署均可由外部存取，仍須依部署網路邊界與執行期路由確認。

### 重要程式證據

- `User.EmployeeId` 為 nullable，模型沒有 `Employee` navigation；DbContext 有 `Username` 唯一索引，未發現 `User.EmployeeId` 唯一索引。
- `Role.RoleName`、`Permission.PermissionCode` 有唯一索引；`UserRole(UserId, RoleId)` 與 `RolePermission(RoleId, PermissionId)` 有複合唯一索引。
- `Employee` 有字串 `Status`（模型預設 `active`）、`DepartmentId`、`SupervisorId`；`Department` 有 `ParentDeptId`，可作部門階層擴充點。
- JWT 目前包含 NameIdentifier、Name、Role 與 `Permission` claims；過期時間使用設定值，未設定時預設 480 分鐘。驗證檢查 issuer、audience、有效期及簽章，ClockSkew 為 0。未發現 token version/security stamp claim 或每次請求查核。
- `PermissionAuthorizeFilter` 只在 endpoint metadata 有 `PermissionAuthorizeAttribute` 時檢查；無該屬性即放行。其權限格式為 `Module.Action` 並接受 `admin.*`，與計畫目標 `module:resource:action` 不同。
- `Program.cs` 註冊 `AddMemoryCache()` 與 `InMemoryCacheService`。原始碼只在註解提及 Redis optional，未發現 Redis client／DI 註冊；不能將 Redis 視為現有可用的撤權依賴。

## 前端與行動 App 呼叫端

- Web client：`src/erp-frontend/src/api/auth.ts` 有登入、註冊與 users／roles／permissions client；`src/erp-frontend/src/api/system.ts` 有權限樹、權限維護、角色查詢、使用者與角色指派及主題 client；`SystemSettings.tsx` 是相關管理 UI。
- 員工 App client：`src/erp-employee-app/src/services/api.ts` 與 `src/erp-employee-app/src/context/AuthContext.tsx` 呼叫登入，並含員工、出勤、請假、薪資與公告等 API 使用端；App request interceptor 會附帶本機 token，收到 401 時清除本機 token／使用者資料。
- 靜態呼叫路徑仍需與實際 API 路由做契約比對；本次未對所有模組 endpoint 逐一建立 1:1 對照。

## 本機延遲與回應基線

測試目標為本機 `http://127.0.0.1:5100` 開發 API；每條路徑序列執行 5 次，量測 PowerShell client 端往返時間。測試使用合成且隨機的不存在帳號，不使用真實員工或帳號資料。

| 路徑／案例 | 樣本數 | 回應 | 最小 ms | 中位數 ms | 最大 ms | 解讀 |
|---|---:|---|---:|---:|---:|---|
| `GET /health` | 5 | 200 × 5 | 38.6 | 39.5 | 73.6 | 本機健康檢查 |
| `POST /api/erp/auth/login`（不存在帳號） | 5 | 401 × 5 | 48.3 | 52.1 | 只量到拒絕登入路徑，不代表成功登入成本 |
| `GET /api/erp/finance/accounting-mappings`（無 token） | 5 | 401 × 5 | 1.7 | 2.2 | 驗證未登入拒絕路徑，不代表已授權查詢成本 |

`http://127.0.0.1:5173/` 回應 200；本次未將前端載入時間當作 API 效能指標。上述樣本數少、環境為本機、沒有正式資料負載，且未取得成功登入／有效 token；不得作為正式 SLA、效能預算或上線容量依據。補測需使用核准的測試帳號與非敏感測試資料，先取得有效 token，再量成功登入及代表性受保護查詢。

## 資料預檢與放行條件

- dbclient 於 2026-10-23 10:29（+08:00）仍回報「Connection not found」，因此改用已確認健康的本機 Docker `erp-postgres` 容器執行；本次資料庫為 `ErpGeneral`。環境檢查未發現 `DB_CONNECTION_STRING`、`ERP_PHASE0_TEST_USERNAME`、`ERP_PHASE0_TEST_PASSWORD` 或 `ERP_PHASE0_TEST_TOKEN` 設定（只檢查是否存在，未讀取任何秘密值）。本機 health endpoint 回應 200，但不代表有效登入／授權測試可執行。
- [employee-identity-preflight.sql](../../../tools/scripts/employee-identity-preflight.sql) 已在唯讀交易中執行並以 `ROLLBACK` 結束；未發現重複 EmployeeId、無效員工連結或 RBAC 孤兒關聯，發現 1 筆啟用帳號未關聯員工。彙總及逐筆覆核要求見 [data-remediation.md](./data-remediation.md)；GUID 明細未複製至一般文件。
- 未新增 EmployeeId 唯一限制、未清理／合併／停用帳號，也未修改任何業務資料。該未關聯帳號待 HR／系統管理者確認是否為核准例外或需修正。
- Phase 2 migration 放行前，須由 HR／系統管理者完成資料異常覆核並簽核處置；由各模組權責主管正式核定 [authorization-matrix.md](./authorization-matrix.md)；並補齊成功登入及有效授權 API 的效能基線與預算。
