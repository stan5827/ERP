# 供應商入口網站規劃

## 1. 白話摘要

供應商入口網站可以理解為外部供應商專屬的「線上自助辦公室」。供應商透過獨立入口處理詢價、訂單確認、出貨通知、品質改善、文件及請款查詢，不需要進入員工 ERP 畫面，也不會直接連線至內網資料庫。

入口網站的安全重點有三項：

- **外部入口隔離**：供應商透過獨立網址，經安全驗證後，只能使用供應商 Portal 的功能。
- **資料依供應商隔離**：後端依登入身分決定所屬供應商。即使竄改網址或資料編號，也不能取得其他供應商資料。
- **延用 ERP 業務資料**：詢價、採購單、收貨及應付帳款仍以 ERP 的正式資料為準，不另建一套容易不同步的系統。

### 五類功能

1. **身分與安全登入**：供應商申請、內部審核、帳號邀請與多因素驗證（MFA）。
2. **詢報價與訂單協作（RFQ／PO）**：查看指派給自己的詢價、提交報價、確認訂單或提出變更需求。
3. **出貨與品質（ASN／CAPA）**：建立出貨通知、填寫適用的批號資料，以及線上回覆品質改善措施。
4. **文件與訊息**：下載公司規範、上傳證書並追蹤效期，透過受控的訊息討論串協作。
5. **請款與對帳查詢**：提交發票請款、查看三方勾稽及付款進度；正式帳務仍由內部核准。

### 四階段推動方式

| 階段 | 重點 | 開放條件 |
|---|---|---|
| 第 0 階段：安全基礎 | 獨立入口、Keycloak／MFA、網路隔離、後端授權、稽核與安全測試。 | 跨供應商越權測試及員工／供應商身分隔離測試通過前，不對外開放。 |
| 第 1 階段：核心協作 | Dashboard、RFQ 報價、PO 接單或提出異議。 | 使用既有 ERP 採購資料，重要回覆具備版本與稽核紀錄。 |
| 第 2 階段：物流與品質 | ASN、CAPA、文件、訊息及到期通知。 | ASN 能接續既有收貨流程，附件具備掃描和授權檢查。 |
| 第 3 階段：財務請款 | 發票送審、三方勾稽、付款狀態查詢。 | 財務確認稅務規則；核准前不得建立正式 AP 或會計過帳。 |

## 供應商入口第一個安全切片

目前已實作第 0 階段的部分程式基礎，但不代表 Portal 已完成安全驗收或可對外開放：

- 員工既有 JWT scheme 保持不變；供應商 API 使用獨立的 `SupplierPortalKeycloak` 驗證 scheme 與 `SupplierPortal` 授權政策。
- 供應商 token 必須由設定的 OIDC authority 驗證，且具備設定的 API audience；未設定 authority 或 audience 時，供應商端點拒絕驗證。
- `SupplierUser.IdentityProviderSubject` 保存外部身分提供者的 `sub` 對照；每次授權都以此對照資料庫，不採用請求中的 `SupplierId`。
- 只有帳號啟用、供應商已核准且供應商狀態為啟用時，才能取得供應商 Portal 範圍。
- `GET /api/supplier-portal/session` 回傳登入者自己的帳號及供應商識別資訊；`POST /api/supplier-portal/invitations/complete` 在 Keycloak 驗證 email、必要操作及 MFA（依設定）完成後啟用 ERP 帳號。
- 員工端可查詢 Portal 帳號、建立 Keycloak 邀請、重試失敗邀請、重寄未完成邀請及取消／停用帳號。舊本地密碼建立／更新端點回傳 `410 Gone`。
- 邀請會在 ERP 寫入不啟用的 `SupplierUser` 後同步呼叫 Keycloak Admin API；成功時以 Keycloak subject 寫入 `IdentityProviderSubject`，失敗時保留 `ProvisioningFailed` 供人工重試。這是同步流程，尚未採用 outbox；若程序在呼叫期間中斷，可能留下 `Provisioning` 狀態，需由維運檢查及後續補上安全復原機制。
- 邀請生命週期 migration `20261027121800_AddSupplierPortalInvitationLifecycle`（含 correlation ID 唯一索引）及供應商協作 migration `20261027130000_AddSupplierPortalCollaboration` 已於 2026-10-27 套用至本機 `ErpGeneral` 資料庫。協作 migration 的 SQL 只新增本階段需要的欄位、資料表與索引，沒有 DROP 操作；此狀態不代表已套用至其他環境。
- `SupplierUser` 既有的密碼雜湊欄位保留以相容既有資料結構；新 Keycloak 邀請帳號不使用本地密碼，新的供應商驗證 scheme 不讀取或驗證該欄位。

供應商 OIDC 設定使用 `SupplierIdentity:Authority` 及 `SupplierIdentity:Audience`；Admin API 另需 `SupplierIdentity:AdminBaseUrl`、`SupplierIdentity:AdminClientId` 與以環境變數注入的 `SupplierIdentity:AdminClientSecret`。可選設定包含 `SupplierIdentity:InvitationLifespanSeconds`（300–604800 秒，預設 86400）及 `SupplierIdentity:RequireTotp`（預設 `true`）。Admin authority 與 Admin API URL 在正式環境強制 HTTPS；僅 `Development` 環境明確啟用 `SupplierIdentity:AllowHttpMetadata` 且兩個 URL 都是 loopback 時允許 HTTP。本機已用一次性 Keycloak realm、public client 與合成供應商帳號完成瀏覽器 OIDC／PKCE 登入、邀請完成及 session 授權測試；此測試不代表公司正式 realm、郵件送達或 MFA 已驗收。機密不得寫入設定檔或版控；外網路由、APISIX／DMZ、TLS 仍待建置與驗收，完成前不得開放公網。

本機 migration 套用後，已使用隔離 PostgreSQL test schema 執行登入鎖定與 JWT／受保護 API 延遲整合測試，兩項皆通過；供應商 OIDC E2E 另使用一次性 PostgreSQL 與 Keycloak 容器驗證未登入 401、已綁定供應商 200、未綁定身分 403。測試容器與資料已於測試結束後清除。

## 第 1 階段：RFQ 詢報價與 PO 回覆進度

已完成後端協作 API 的第一個可驗證切片，仍未代表 Portal 前端或外部上線已完成：

- RFQ 供應商指派沿用既有 `Rfq`、`RfqSupplierResponse` 與採購供應商主檔；內部採購可用 `PUT /api/erp/rfqs/{id}/suppliers` 指派已核准且啟用的供應商。Portal 只列出明確指派且未撤回的 RFQ。
- 報價使用既有 `RfqSupplierResponse`，檢查指派關係、RFQ 狀態／截止時間及品號歸屬；不會接受前端傳入 SupplierId。重送報價會更新該供應商既有品項報價，不保留歷次報價版本，因此「不可變更報價版本歷程」仍是未完成項目。
- Portal 可查詢本供應商採購單，並提交接受、拒絕或逐品項變更提案。回覆與替代數量／交期另存歷程，記錄回覆者、提交時間及採購單版本；不直接改寫 PO 正式內容或既有收貨流程。
- `SupplierPortalCollaborationServiceTests` 涵蓋 RFQ 指派範圍、未受邀拒絕、供應商報價隔離、PO 跨供應商拒絕、逐品項變更歷程、原因必填及回覆角色限制。
- 新增 schema migration 已套用至本機 `ErpGeneral`。`SupplierPortalCollaborationMigrationIntegrationTests` 在隔離 PostgreSQL schema 實際執行 migration 升級與回滾，驗證三張資料表、RFQ 報價新增欄位及 migration history。
- 目前尚無已串接 Keycloak OIDC 的 Portal 前端；現有 Portal 頁面仍需完成登入整合及 RFQ／PO 操作介面。此 API 切片不可視為可供供應商直接操作或對外開放。

## Keycloak 供應商帳號邀請與綁定流程

### 設計目標與權責邊界

- **ERP 是供應商關係與業務授權的權威來源**：保存 `SupplierUser`、`SupplierId`、Portal 角色、管理者核准、帳號是否可用，以及邀請案件的稽核狀態。
- **Keycloak 是登入憑證與驗證強度的權威來源**：建立外部身分、寄送一次性設定連結、管理密碼／MFA／登入狀態及登出工作階段。ERP 不接收、產生或保存供應商密碼，也不自行簽發供應商登入 token。
- 邀請與身分綁定由 ERP 後端呼叫 Keycloak Admin REST API 完成；瀏覽器與供應商不得取得 Keycloak 管理 token 或直接操作管理 API。
- 既有 ERP 員工 JWT 不參與供應商邀請、登入或綁定；也不得因 email、username 相同而自動把員工身分連結成供應商帳號。

### 建議帳號生命週期

| ERP 邀請狀態 | 意義 | Portal 登入 |
|---|---|---|
| `PendingApproval` | 供應商或聯絡人尚未經內部核准 | 拒絕 |
| `ReadyToInvite` | 供應商已核准，等待授權管理者確認受邀 email、角色與供應商歸屬 | 拒絕 |
| `Provisioning` | ERP 已記錄邀請意圖，正在建立／比對 Keycloak 使用者 | 拒絕 |
| `InvitationSent` | Keycloak 帳號已建立並綁定 subject，設定密碼及 MFA 的限時連結已寄出；ERP 使用者仍未啟用 | 拒絕 |
| `Active` | 受邀人完成 Keycloak 必要設定，並經 ERP 首次登入／完成邀請檢查啟用 | 僅可使用綁定供應商範圍 |
| `Expired`／`Cancelled`／`Suspended` | 邀請逾期、被撤銷或帳號被停用 | 拒絕 |
| `ProvisioningFailed` | Keycloak 呼叫或 ERP 綁定失敗，等待可稽核的重試／人工處置 | 拒絕 |

狀態是 ERP 的授權閘門，Keycloak 的 user enabled、email verified、required actions、MFA 與 session 狀態則是登入條件；兩邊都符合才可使用 Portal。不要只憑 Keycloak 帳號存在就視為 ERP 已授權。

### 邀請與綁定步驟

1. **內部核准**：供應商主檔須為 `ApprovalStatus=APPROVED` 且 `Status=ACTIVE`。具備明確供應商帳號管理權限的員工，選擇既有供應商、受邀人姓名、email 及最小權限 Portal 角色。供應商不得自行指定 `SupplierId` 或升級角色。
2. **建立邀請意圖**：ERP 驗證 email、角色、供應商狀態及重複邀請；產生不可預測的邀請案件識別碼與到期時間，建立 `SupplierUser`／邀請紀錄為未啟用狀態。資料庫交易只寫入 ERP 狀態與待處理 outbox 工作，不在資料庫交易內直接呼叫外部服務。
3. **建立／邀請 Keycloak 使用者**：背景處理器以專用 confidential service account 取得短效 Admin API token，並在指定 realm 建立使用者。設定 `emailVerified=false` 及受控的 ERP 邀請 correlation attribute，加入 `VERIFY_EMAIL`、`UPDATE_PASSWORD`、`CONFIGURE_TOTP` 等經核准的 required actions，再由 Keycloak 寄出限時設定信。Keycloak 帳號啟用狀態須依所選版本的邀請流程要求設定；無論 Keycloak 是否允許使用者開始登入，ERP 的未啟用狀態都必須阻止其呼叫 Portal API。邀請信 URL、token、密碼與 Admin API token 不得寫入 log、一般 outbox payload 或 API 回應。
4. **固定身分對照**：成功建立後，ERP 取得 Keycloak 回傳的 subject（`sub`），以唯一索引寫入 `SupplierUser.IdentityProviderSubject`，並將 ERP 邀請狀態更新為 `InvitationSent`。email 只用於送邀請及核對，不是登入後的授權查找鍵；不因 email 相同自動綁定既有 Keycloak 帳號。若 subject 已綁定其他 ERP 帳號，必須停止流程並告警，禁止覆寫。
5. **供應商完成設定**：受邀人使用 Keycloak 寄送的限時連結驗證 email、設定自己的密碼與 MFA。密碼政策、MFA 必填方式、連結有效期與重寄上限由 Keycloak realm policy 管理；ERP 不建立自製密碼設定頁或接受密碼內容。
6. **首次登入與 ERP 啟用**：Portal 使用 OIDC Authorization Code + PKCE 登入。API 驗證 issuer、audience、簽章、有效期限及受核定的 email-verified／MFA claim 契約，再用 `sub` 尋找唯一 ERP 帳號。對 `InvitationSent` 帳號只允許呼叫受限的完成邀請流程；完成條件全部成立後，ERP 記錄啟用時間並將狀態改為 `Active`。後續每次 API 請求仍即時確認 ERP 帳號與供應商均有效，並依供應商範圍授權。
7. **管理變更與撤銷**：內部管理者可重寄（先使舊邀請失效）、取消、停用或重新邀請；每個動作皆要記錄執行者、原因、時間、結果與目標 subject。停用時先讓 ERP 授權立即失效，再停用 Keycloak 帳號並撤銷其 sessions／refresh tokens；Keycloak 操作失敗需告警與重試，不能恢復 ERP 存取權。

### 故障復原、重試及稽核

- Keycloak 是外部系統，邀請採 **transactional outbox + 背景重試**，以 ERP 邀請 ID 作冪等鍵；不得因 HTTP timeout 就直接新增另一個使用者。
- 若 Keycloak 已建立帳號但 ERP 寫入 subject 失敗，先以受控 correlation attribute 查回原使用者並完成綁定；無法確認唯一匹配時轉 `ProvisioningFailed` 並人工處理，不能依 email 模糊比對或自動連結。
- 邀請到期、取消、重寄及帳號停用均需讓 ERP 端立即拒絕存取。同步停用或 session 撤銷若失敗，保留失敗狀態供重試並告警；不要因 Keycloak 暫時不可用而 fail-open。
- 需保存邀請案件的狀態歷程、操作者、供應商與使用者關聯、Keycloak subject、建立／寄送／接受／到期／撤銷時間、重試次數及錯誤分類。不得保存一次性 action token、登入 token、密碼或 MFA secret。
- 先盤點既有稽核模型能否安全記錄外部身分事件；若沒有合適結構，再新增最小化的 Portal identity event／outbox schema，避免直接將供應商身分事件錯記成員工 `UserId`。

### 既有帳號管理的遷移原則

目前 `SuppliersController` 的舊 Portal user 建立／更新路由已停用並回傳 `410 Gone`，供應商管理頁已改用 Keycloak 邀請、狀態、重試、重寄及停用操作。既有密碼 hash 不做明文轉移、不匯入 Keycloak；仍須盤點其他可能的本地密碼驗證呼叫端及既有帳號，確認切換後沒有其他 API 可驗證舊本地密碼。

### 本流程的建議驗收條件

- 未核准或已停用的供應商，無法建立可用邀請；只有具備供應商帳號管理權限的員工能邀請。
- 受邀 email 尚未驗證、密碼設定／MFA 未完成、邀請逾期／取消，或 ERP 狀態非 `Active` 時，均無法呼叫 Portal 業務 API。
- 綁定以經驗證 token 的 `sub` 唯一查找；任意 email、`SupplierId`、使用者 ID 或 request body 值都不能建立或改變綁定。
- 已被其他 ERP 帳號綁定的 Keycloak subject 無法再次綁定；重寄邀請不會重複建立使用者，舊連結失效。
- Keycloak timeout、重試、重複工作、Keycloak 建帳後 ERP 暫時失敗，以及停用／撤銷 API 失敗都維持 fail-closed，並可恢復或告警。
- 稽核與一般應用 log 不含 action token、access／refresh token、密碼、MFA secret 或過度暴露的個人資訊。

## 2. 推動建議與專案行動

### 2.1 先確認唯一的正式資料來源

目前系統已有重疊的報價和 ASN 資料模型。正式開發前，先確認現有資料、API 與使用端，並選定每類業務資料的唯一來源；Portal 應透過 API 和專用 DTO 協作，不應維護一份平行的採購或會計資料。

建議直接沿用成熟的 ERP 核心採購、收貨與財務模型。若評估後確有新需求，再以 EF Core migration 擴充必要欄位與流程，避免同步衝突及重複維護。

### 2.2 將後端授權視為最高防線

供應商的 `SupplierId` 必須從伺服器端已驗證的登入身分查出，不能由前端 URL、查詢參數或表單內容決定。這項檢查必須涵蓋讀取、修改、上傳及下載；不能只依賴 UI 隱藏功能或單一 API 中介層。

所有供應商 API 都應通過一致的身分解析和服務層範圍授權，並使用跨供應商測試驗證：供應商 A 嘗試讀取或更改供應商 B 的資料時，操作必須被拒絕，而且不得洩露 B 的資料是否存在。

### 2.3 嚴守第 0 階段安全門檻

在 Keycloak 整合、外網路由與網域隔離、MFA、帳號停用／撤銷，以及跨供應商越權測試都完成並驗收前，不應把 Portal 部署至公開網際網路。

第 0 階段的完成條件應包括：供應商與員工身分互斥、供應商範圍測試通過、HTTPS／DMZ／網關設定完成、憑證及金鑰有管理程序，且安全事件具備稽核和告警。

### 2.4 提早確認財務與稅務政策

即使發票請款安排在第 3 階段，仍建議在第 1 階段進行時就由財務／會計確認請款必備欄位、發票格式、營業稅處理、三方勾稽容差及付款狀態的揭露方式，降低後期變更風險。

供應商提交請款不等於正式 AP 已成立。請款需經驗證、勾稽與內部核准後，才能建立／更新正式應付帳款；未核准的請款不能觸發會計過帳或付款。

### 2.5 建議近期行動

1. 指定採購、資訊安全、財務及維運窗口，確認第 0 階段負責人及驗收人。
2. 完成 Keycloak、APISIX、公開網域、DMZ、TLS、郵件／MFA 服務的架構與維運決策。
3. 確認 Keycloak realm、Portal OIDC client、Admin API service account、MFA policy、郵件來源及管理權限最小化方式。
4. 盤點既有 Portal user 管理 UI／API 與密碼帳號，制定重新邀請和停用舊登入方式的切換計畫。
5. 盤點報價、ASN 的重複模型與現有資料，列出唯一來源及必要遷移策略。
6. 先完成邀請狀態／outbox schema、管理操作授權、subject 綁定與撤銷測試，再實作供應商業務 API。

## 3. 目的與規劃原則

本文件規劃 ERP 供應商入口網站（Supplier Portal），讓外部供應商能在不進入員工 ERP 畫面的前提下，安全地協作詢報價、採購訂單、出貨通知、品質改善、文件及帳款查詢。

規劃以目前系統的 ASP.NET Core 8、EF Core、PostgreSQL 和既有採購、收貨、品質、財務流程為基礎，遵循以下原則：

1. **重用既有業務主檔與流程**：不另建重複的採購單、收貨單、應付帳款或供應商主檔。
2. **外部身分與員工身分分離**：供應商使用獨立的登入、權限、API 與入口路由。
3. **授權範圍由後端決定**：API 必須從已驗證的供應商身分解析 `SupplierId`，不得信任客戶端傳入的供應商識別值。
4. **逐階段開放**：先完成身分、安全隔離及 RFQ／PO 協作，再擴充 ASN、品質、文件與請款。
5. **敏感作業須可稽核**：保留操作人、時間、狀態、版本及重要欄位的變更紀錄。

## 4. 現況盤點

| 領域 | 系統現況 | 規劃影響 |
|---|---|---|
| 供應商主檔與審核 | [ProcurementModels.cs](../../src/ErpBackend/Models/Procurement/ProcurementModels.cs) 已有供應商統編、聯絡資料、狀態與審核欄位；[SuppliersController.cs](../../src/ErpBackend/Controllers/SuppliersController.cs) 提供內部供應商管理、審核及入口帳號管理端點。 | 延用供應商主檔及內部審核流程。外部申請須先進入待審狀態，不能直接建立可登入帳號。 |
| 供應商帳號 | [PortalModels.cs](../../src/ErpBackend/Models/Portal/PortalModels.cs) 有 `SupplierUser`；新增 `IdentityProviderSubject` 作為 OIDC `sub` 對照。 | [SupplierPortalController.cs](../../src/ErpBackend/Controllers/SupplierPortalController.cs) 僅提供經 Keycloak policy 保護的 session 身分查詢。[SupplierAuthController.cs](../../src/ErpBackend/Controllers/SupplierAuthController.cs) 仍只有 `ping`；Keycloak 邀請、MFA、帳號重設與員工管理流程尚未實作。 |
| 供應商入口 UI | [SupplierPortal.tsx](../../src/erp-frontend/src/pages/SupplierPortal.tsx) 有登入、RFQ 與 ASN 清單的 UI 雛形。 | 目前屬於員工 ERP 前端模組，且其 `/supplier-portal/...` API 呼叫尚無相對應的完整 Portal API。可重用 UI 與需求探索成果，但正式對外前應分離入口與認證狀態。 |
| RFQ 報價 | [ProcurementModels.cs](../../src/ErpBackend/Models/Procurement/ProcurementModels.cs) 已有 `Rfq`、`RfqItem`、`RfqSupplierResponse`；[PortalModels.cs](../../src/ErpBackend/Models/Portal/PortalModels.cs) 另有 `SupplierQuote`。 | 兩套報價模型需先確認使用狀況並選定唯一正式寫入來源。供應商只能看到明確指派給自己的 RFQ，不能透過傳入其他 `SupplierId` 取得資料。 |
| 採購單與出貨通知 | 採購模型已有 `PurchaseOrder`、`PurchaseOrderItem`、`Asn`、`AsnItem`，包含批號、製造日期及有效期限等資料；另有 Portal 專用的 `SupplierAsn`、`SupplierAsnItem`。 | 應以可串接既有收貨流程的採購 ASN 作為正式來源。整併前先確認現有資料及呼叫端，避免建立第二套出貨事實來源。 |
| 品質與文件 | [QaModels.cs](../../src/ErpBackend/Models/QA/QaModels.cs) 有供應商客訴、CAPA、審查與附件 metadata；[PortalModels.cs](../../src/ErpBackend/Models/Portal/PortalModels.cs) 有供應商文件、文件效期、Portal 訊息與討論串。 | [SupplierComplaintsController.cs](../../src/ErpBackend/Controllers/SupplierComplaintsController.cs) 提供員工端 CAPA 審查及附件清單／下載／重掃；[SupplierPortalCapaController.cs](../../src/ErpBackend/Controllers/SupplierPortalCapaController.cs) 提供供應商端 CAPA 附件操作。 |
| 應付帳款 | `AccountsPayable` 已記錄供應商發票、稅額、付款狀態和三方勾稽狀態；[AccountsPayableController.cs](../../src/ErpBackend/Controllers/AccountsPayableController.cs) 屬內部帳務 API。 | 外部請款應先進入獨立送審流程。未完成驗證與內部核准前，不得建立正式 AP 或產生會計過帳。 |
| 身分驗證與部署 | [Program.cs](../../src/ErpBackend/Program.cs) 使用 ERP 的 JWT 驗證流程。現有 [docker-compose.yml](../../deployment/docker-compose.yml) 未配置 Keycloak 或 APISIX。 | Keycloak 與 APISIX 屬目標架構，必須規劃新增部署、金鑰管理、TLS、網路隔離及操作監控；目前不能視為已建置。 |

## 5. 目標架構

```text
供應商瀏覽器
    │ HTTPS、MFA
    ▼
DMZ：APISIX API Gateway
    ├── /supplier      → 獨立供應商 Portal 前端
    └── /api/supplier  → 供應商專用 API
                              │ 私有網路、限縮權限
                              ▼
                        ERP 應用服務
                              │
                              ▼
                        PostgreSQL

Keycloak：供應商身分、邀請、登入、MFA、停用與撤銷
```

### 3.1 網路與入口

- 員工繼續使用內部 ERP 網址；供應商使用獨立入口網址，例如 `https://portal.example.com.tw/supplier`。
- 外部流量只經 DMZ 的 APISIX 進入指定 Portal 路由，不開放供應商直接連入 ERP 內網或資料庫。
- 對外入口一律使用 HTTPS；APISIX 負責路由、TLS 終止或轉送、限流、請求大小限制及必要的來源網路政策。
- 供應商 API 使用獨立路由與授權政策，不直接暴露內部管理用採購、AP、供應商 CRUD API。
- Portal 前端可以與員工前端共用視覺元件或設計 token，但不得共用員工登入 token、會話或管理功能。

### 3.2 身分與權限

- Keycloak 作為供應商登入與 MFA 的身分提供者，支援首次邀請、密碼設定、TOTP 或經核准的 Email OTP、帳號停用及工作階段撤銷。
- 內部人員審核供應商申請及邀請帳號；供應商核准與入口帳號啟用需有可追溯的審核紀錄。
- `SupplierUser` 綁定外部身分識別（例如 Keycloak subject）、內部 `SupplierId`、入口角色及狀態；不把供應商登入密碼存於 ERP。
- 供應商角色可先採 `supplier.admin`、`sales`、`finance` 等最小集合；帳號角色只能授予該供應商範圍內的能力。
- ERP API 必須驗證 issuer、audience、簽章、有效期限及必要 claims，並明確區隔員工與供應商 token。

### 3.3 供應商資料隔離

- 每個供應商 API 由驗證後的 token subject 對應到啟用中的 `SupplierUser` 和 `SupplierId`。
- 不接受客戶端自選的 `SupplierId` 作為資料範圍依據；若路由或請求 DTO 包含該欄位，必須移除或驗證與身分對應完全一致。
- 查詢、建立、更新、刪除、附件上傳及附件下載都必須套用同一供應商範圍檢查。
- 未授權存取其他供應商資料時，回應不得透露資料是否存在；同時記錄安全稽核事件。
- PostgreSQL Row-Level Security 可作為縱深防禦，但須先設計交易層設定、連線池重用和背景工作身分，避免 session context 洩漏；不得以 RLS 取代應用程式的授權。

## 6. 功能範圍

### 4.1 供應商申請、審核及帳號生命週期

1. 供應商提交公司名稱、統一編號、主要聯絡人及 Email。
2. 系統驗證必填欄位及統編格式，建立待審申請，不開放登入。
3. 內部採購／供應商管理人員比對既有供應商主檔，決定核准、退回或拒絕。
4. 核准後，由 Keycloak 邀請使用者設定密碼及 MFA；ERP 記錄供應商與外部身分的關聯。
5. 離職、聯絡人異動、供應商停用或安全事件發生時，能撤銷帳號、token／session 並留下稽核紀錄。

### 4.2 Dashboard 與待辦摘要

登入後只呈現該供應商的工作摘要：

- 待回覆 RFQ 數量及近期截止項目。
- 待確認採購單及待協商項目。
- 待提交／即將到貨 ASN。
- 待回覆客訴與 CAPA 截止日。
- 請款審核、付款狀態及預計付款日。
- 公司文件與證書到期提醒。

Dashboard 指標均由後端依已驗證的供應商範圍計算，不接受前端提供供應商 ID。

### 4.3 RFQ 詢價與報價

供應商只能檢視 ERP 已指派給該供應商且仍在有效期限內的詢價資料。

報價欄位依採購政策確認後納入：

- RFQ 編號、發出日期、報價截止時間、貿易條件。
- 品號、品名／規格、需求數量、期望交期。
- 單價、報價幣別、承諾交期、MOQ、包裝倍數及備註。
- 必要時附加報價文件或規格偏差說明。

報價提交後須記錄提交人、提交時間及版本；截止後不得一般性覆寫，修訂應建立新版本或由內部人員重新開放。內部採購比較報價並選定供應商，外部 Portal 不提供其他供應商報價或排名。

### 4.4 採購訂單回覆

- 供應商可檢視授權範圍內已發出的採購單及明細。
- 「確認接單」記錄使用者、時間、訂單版本及接受狀態。
- 「要求變更／拒絕」須提交原因及可行的替代交期／數量，送交內部採購協商。
- 外部回覆不能直接改寫已核准 PO 的金額、幣別、明細或付款條件。正式變更仍由內部採購人員經既有核准流程處理。
- PO 版本更新後應要求供應商重新確認重要變更，避免回覆與不同版本的訂單混淆。

### 4.5 ASN 出貨通知

- ASN 必須連結已確認採購單及明細，並檢查出貨數量不超過未交數量或核准的超交容差。
- 記錄預計到貨日期、承運商、追蹤號碼、出貨備註。
- 依品項或廠內規範要求批號、製造日期、效期；必要欄位由品項設定決定，不應無條件要求所有品項填有效期限。
- ASN 應由現有收貨流程接續使用，避免建立無法轉成收貨單的 Portal 專用資料。
- QR／條碼箱標及棧板標籤須先定義格式、唯一編碼、列印規格及掃描流程，再列入實作範圍。

### 4.6 客訴與 CAPA

- 供應商只可讀取指派給自身的客訴及 CAPA。
- 供應商依期限填寫適用的 D1–D8 矯正預防措施、附件和補充說明。
- 系統記錄每次回覆、修訂及內部審查結果，不覆蓋歷史內容。
- 狀態、截止日、逾期提醒與結案權限由內部品質流程控制。

#### 內部品質審查／退回後端

內部審查已使用員工認證，與供應商 Keycloak scheme 分離；讀取端點要求 `quality:view`，開始審查、核准／退回、結案及重掃要求 `quality:approve`。審查者 ID 取自驗證後的員工 claims，不接受 request body 指定。

CAPA 狀態轉移如下，審查結果及意見寫入只新增的 `SupplierCapaReview` 歷程並綁定回覆 revision：

| 目前狀態 | 操作者與動作 | 下一狀態 | 規則 |
|---|---|---|---|
| `open`／`in_progress` | 供應商保存草稿 | `in_progress` | 僅更新尚未提交的回覆；每次儲存保留回覆版本。 |
| `in_progress`／`revision_requested` | 供應商提交 D1–D8 | `submitted` | 必須有完整回覆；提交時固定本次 revision，不能事後改寫。 |
| `submitted` | 品質人員開始審查 | `under_review` | 只能審查最新提交 revision；同一時間不可重複建立待審案件。 |
| `under_review` | 品質人員核准 | `approved` | 必須記錄核准者、時間、revision 及審查意見；只有品質權限可執行。 |
| `under_review` | 品質人員退回補正 | `revision_requested` | 退回原因必填，回覆者才可再編輯與重新提交。 |
| `approved` | 品質主管結案 | `closed` | 核准與主管結案分開，保留效果確認與結案說明。 |

員工端 API 為 `GET /api/erp/supplier-complaints`、`GET /api/erp/supplier-complaints/{complaintId}`、`GET .../{complaintId}/capa/reviews`、`POST .../{complaintId}/capa/reviews/start`、`POST .../{complaintId}/capa/reviews`、`POST .../{complaintId}/capa/close`。案件清單有伺服器端頁碼／筆數限制；審查決策須附正在審查的 revision number，過期版本回 `409`；退回原因及結案說明必填。狀態更新與審查紀錄採同一資料庫交易。

供應商收到退回後仍使用既有 `PUT .../capa` 儲存補正草稿與 `POST .../capa/submit` 重新提交；每次重新提交新增 revision，並使上一輪審查仍可追溯。狀態衝突或過期 revision 回 `409`，不覆蓋新版資料。

#### CAPA 附件 API 與儲存

附件本體使用 MinIO／S3 相容 private bucket，資料庫只保存不可推測 storage key 及 metadata；預設不提供 public URL。AWS SDK S3 client 設定 MinIO path-style，Production 強制 HTTPS，只有 Development 明確允許 loopback HTTP 才可使用明文連線。桶須由維運預先建立並設為 private，使用專用最小權限 access key。

供應商端點為 `POST /api/supplier-portal/complaints/{complaintId}/capa/attachments`、`GET .../{complaintId}/capa/attachments`、`GET .../{complaintId}/capa/attachments/{attachmentId}/content` 及 `DELETE .../{complaintId}/capa/attachments/{attachmentId}`。品質人員可使用員工端 `GET /api/erp/supplier-complaints/{complaintId}/capa/attachments` 清單／下載，另有 `POST .../{complaintId}/capa/attachments/{attachmentId}/rescan` 重試掃描。所有端點先驗證 complaint／supplier／attachment 關聯；資料庫不回傳實體 storage path。

上傳限制每檔 20 MB、每 CAPA 10 個未撤銷附件，只允許 PDF／JPEG／PNG，檔案副檔名須符合 magic bytes。伺服器產生 quarantine key，寫入 `pending_scan` metadata 與 private bucket 後，同步呼叫 ClamAV `INSTREAM`；掃描成功且 clean 才回 `201`，並可下載及綁定提交的 response revision。感染檔標為 `rejected`、移除物件並回 `422`；掃描器不可用或設定不完整標為 `scan_failed` 並回 `503`，不開放下載或 CAPA 提交，品質人員可重掃。供應商只可撤銷尚未提交的附件；已綁定回覆版本的附件不可改寫或撤銷。設定未齊、MinIO／ClamAV 尚未部署時，附件 API 回服務不可用，不會使用本機目錄作成功形 fallback。

已新增 `SupplierCapaReview`／`SupplierCapaAttachment` 模型、`20261027170100_AddSupplierCapaReviewAndAttachments` migration、員工品質審查 API、供應商附件 API、MinIO／ClamAV adapter、私有下載／重掃及服務測試。尚待設定實際 MinIO／ClamAV endpoint 與 secrets、執行 PostgreSQL migration 整合測試、部署附件服務及完成 Portal UI 與外部安全測試；未完成前不可開放公網附件功能。

### 4.7 文件與訊息

- 提供採購條款、包裝規範及供應商行為準則等授權下載文件。
- 供應商上傳 ISO、SGS、RoHS 等證書及相關文件，記錄文件類型、版本、有效期限、審核狀態和所屬供應商。
- 檔案儲存在受控檔案儲存區；資料庫只存 metadata 與安全的儲存識別值，不直接存任意本機路徑或公開 URL。
- 檔案下載須先驗證供應商權限，再由後端串流或產生短效存取連結；依政策執行檔案類型／大小限制、惡意程式掃描及存取稽核。
- 文件效期通知需保存排程、寄送、重試及失敗紀錄；初始提醒點可採到期前 30 天，實際寄送方式依郵件服務政策確認。
- Portal 訊息沿用討論串概念，必須綁定供應商及經授權的業務單據。

### 4.8 發票請款與付款查詢

- 供應商提交發票號碼、發票日期、幣別、未稅金額、稅額、含稅總額及憑證檔案，並關聯已完成收貨／驗收的單據。
- 供應商請款送審應與正式 `AccountsPayable` 區分，記錄送審、退件、補件、核准及拒絕歷程。
- 系統核對 PO、收貨／驗收與發票數量、金額及稅額；差異依政策退件或送人工審核。
- 經內部核准後才建立／更新正式 AP；外部 Portal 不可建立付款批次或觸發會計過帳。
- 供應商可查看自己的請款狀態、核准待付款金額與已付款資訊；預計匯款日須來源明確且由財務確認後再顯示。
- 台灣電子發票字軌、XML 規格、稅額與會計處理須由財務確認後再納入需求。
- 供應商請款送審的資料模型與 API 規格見[AP Submission 規格](./ap-submission-api-spec.md)，Portal 操作介面另見[請款送審 UI 計畫](./ap-submission-portal-ui-plan.md)。案件、修訂、明細、附件及審核紀錄的 EF Core entities 與 migration 已建立；PostgreSQL 隔離 schema migration 與 AP 核准流程測試已通過，但尚未套用至業務資料庫，Portal UI 尚未實作。財務、採購及資安政策仍待確認。

#### AP 查詢基礎修正進度

- 內部 AP 摘要、帳齡與供應商明細已改以既有 `AccountsPayable`、`PaymentRecord`、`PaymentBatch` 及有效已過帳借項為依據，不再以 PO 收貨累計金額推算應付或已付款。
- 付款查詢僅將 `posted` 付款批次計入實際付款；折讓與借項分開回傳，並一併扣減未付餘額。舊 `PurchaseOrder.ReceivedAmount` 僅在付款登錄相容檢查時用來辨識尚未連結到 AP 紀錄的 legacy 餘額，不作為查詢實付金額來源。
- 帳齡依 AP `DueDate` 分為未到期／無到期日、逾期 1–30、31–60、61–90 及 90 天以上；摘要的起訖日期依供應商發票日期篩選。
- AP 控制器及付款業務規則的 SQLite 整合測試涵蓋摘要、帳齡、明細及付款批次狀態；另以一次性 PostgreSQL 16 容器及隨機隔離 schema 完成 AP 查詢整合測試，驗證發票日期篩選、AP 摘要、已過帳付款、折讓與帳齡桶。測試發現並修正日期條件需使用 UTC `DateTime` 才符合 PostgreSQL `timestamp with time zone`；測試 schema 與容器已清除，未操作正式或 staging 資料庫，也未套用資料庫 migration。財務確認（發票／PO 限制、容差、電子發票與稅務規則）仍待完成；此修正尚未實作供應商請款送審流程或 Portal 付款查詢畫面。

## 7. 資料模型與 API 原則

### 5.1 資料模型取捨

本專案使用 EF Core + PostgreSQL，使用 EF Core migration 管理 schema，不採 Prisma schema。新增資料前先盤點既有模型、資料及呼叫端。

| 模型／資料 | 規劃 |
|---|---|
| `Supplier` | 延用現有供應商主檔與內部審核欄位。公開申請若需暫存完整資料，使用待審申請流程並由內部人員核准、比對或轉入主檔。 |
| `SupplierUser` | 延用為 ERP 內的外部身分對照與授權資料；subject 唯一且只由受控邀請流程寫入。帳號狀態、邀請／啟用資訊及 Keycloak 操作歷程需先盤點現有稽核模型，再決定最小 schema；Keycloak 為密碼和 MFA 的權威來源。 |
| Keycloak 邀請工作 | ERP 以 outbox／背景工作呼叫 Keycloak Admin REST API，保存冪等邀請案件、重試狀態及安全稽核；不把 Admin token 或 action token 寫入 ERP 資料表。 |
| `RfqSupplierResponse`／`SupplierQuote` | 確認現況使用後選定唯一正式報價來源；再補足必要的幣別、承諾日期、包裝倍數、報價版本及附件關聯。 |
| `Asn`／`AsnItem` 與 `SupplierAsn`／`SupplierAsnItem` | 確認既有資料及 API 使用情況，採可供收貨流程承接的一套資料來源；依需求補足外部提交者、回覆狀態與稽核資訊。 |
| `PortalMessageThread`／`PortalMessage` | 延用討論串模型，檢視附件存放、長度限制、供應商範圍與稽核欄位，不另建平行訊息資料表。 |
| `SupplierDocument` | 延用文件 metadata 概念，確認檔案儲存識別、版本、審核狀態、到期通知紀錄與存取政策。 |
| `AccountsPayable` | 延用正式帳務資料。請款送審在核准前另有明確的暫存／工作流程，不允許供應商直接修改正式 AP。 |
| 申請、回覆及版本稽核 | 先確認系統是否已有可重用的工作流程／稽核模型；只為必要的生命週期、版本或財務核准新增 schema。 |

### 5.2 API 邊界

- 建立供應商專用路由，例如 `/api/supplier/...`，並採獨立政策或等效的身分驗證界線。
- 員工管理 API 與供應商 Portal API 分開設定授權；不要把內部 CRUD endpoint 直接加上外部可達路由。
- Portal API 從 claims 對應到內部 `SupplierUser`，再由服務層執行供應商範圍限制。
- POST／PATCH 動作使用專用 DTO；不要讓外部直接提交資料庫 entity、內部狀態、核准人、supplier scope 或會計欄位。
- 重要動作需有冪等或重複提交保護、明確錯誤狀態、速率限制及稽核紀錄。
- 所有外部請求需套用輸入驗證、大小限制、取消訊號及安全錯誤回應，不回傳內部堆疊或其他供應商資訊。

## 8. 分階段實施計畫

### 第 0 階段：安全與流程基礎

1. 確認外部網址、TLS 憑證、DMZ 網段、APISIX／Keycloak 部署方式及郵件／OTP 服務。
2. 確認 Keycloak realm／client、管理 service account 權限、email 服務、required actions 與 MFA claim 契約。
3. 定義供應商申請、核准、邀請、MFA、重設、停用與緊急撤銷流程，並替換既有本地密碼帳號管理。
4. 建立邀請 outbox／重試、Keycloak subject 綁定、首次登入啟用、取消與撤銷流程。
5. 將供應商 Portal 從員工 ERP 模組頁分離，避免共用登入狀態。
6. 建立獨立供應商 token 驗證與 API 授權政策，定義員工和供應商 token 的互斥邊界。
7. 建立身分到 `SupplierId` 的後端解析，以及服務層隔離測試。
8. 設定外部流量路由、限流、日誌、監控、憑證管理與安全告警。

**階段完成門檻：**外部入口尚未開放前，需先通過邀請重試／過期／撤銷測試、subject 綁定測試、跨供應商存取測試、員工／供應商 token 隔離測試及 MFA 測試。

### 第 1 階段：身分、Dashboard、RFQ 與 PO 回覆

- 供應商申請、內部核准、Keycloak 邀請及 MFA。
- 供應商專屬 Dashboard 與待辦摘要。
- RFQ 查看、報價提交、版本與截止規則。
- PO 查看、接受、要求變更或拒絕；由內部流程處理正式採購單修改。
- 內部人員可查看供應商回覆、稽核歷程及待協商事項。

### 第 2 階段：ASN、品質、文件與訊息

- ASN 建立與既有收貨流程串接、超交檢查及批號／效期驗證。
- 供應商客訴回覆與 CAPA 追蹤。
- 文件下載、證書上傳、到期通知與檔案掃描。
- 依單據授權的 Portal 訊息和附件交換。
- QR／條碼標籤在規格、箱號及倉庫掃描流程確認後實作。

#### ASN 後端首批交付

- 新增受 `SupplierPortal` policy 保護的 ASN 清單、明細、可出貨數量查詢及提交端點：`GET /api/supplier-portal/asns`、`GET /api/supplier-portal/asns/{asnId}`、`GET /api/supplier-portal/purchase-orders/{purchaseOrderId}/asn-options`、`POST /api/supplier-portal/asns`。
- ASN 寫入既有採購 `Asn`／`AsnItem`，不使用並行的 Portal 專用 ASN 表；supplier ID 一律取自已驗證的 Portal 身分，不接受 request body 指定。
- 只允許供應商提交已確認或部分收貨且未鎖定的自有 PO；拒絕非 PO 品號、零／負數量、超過 PO 未收量、已由其他有效 ASN 保留的數量，以及不合邏輯的出貨／到貨／製造／效期日期。
- `LotControlled` 品項需提供批號、製造日期及有效期限。ASN 使用 `submitted`／`partially_received`／`received` 狀態；內部收貨 API 可帶 ASN ID，會驗證同一 PO／供應商及未收出貨量、將收貨單關聯至 ASN 並更新其收貨進度。
- 已新增 ASN 服務及既有收貨流程的測試，涵蓋供應商隔離、超量／保留數量拒絕、批號欄位與收貨關聯；API 範例見 [SupplierPortalAsn.http](../../tests/ErpBackend.IntegrationTests/SupplierPortalAsn.http)。
- 目前只完成 API／服務與收貨關聯；Portal ASN 操作畫面、ASN 撤回／變更流程、QR／標籤列印、跨系統正式環境與瀏覽器端到端驗收仍未完成。公開網路部署前仍須通過第 0 階段安全門檻。

#### CAPA 後端首批交付

- 新增受 `SupplierPortal` policy 保護的客訴清單／明細、CAPA 草稿儲存及 D1–D8 提交端點：`GET /api/supplier-portal/complaints`、`GET /api/supplier-portal/complaints/{complaintId}`、`PUT /api/supplier-portal/complaints/{complaintId}/capa`、`POST /api/supplier-portal/complaints/{complaintId}/capa/submit`。
- 沿用既有 `SupplierComplaint`／`SupplierCapa` 主檔；供應商範圍只由已驗證 Portal 身分取得。只允許 `sales` 角色修改，跨供應商讀寫均回傳不存在，不洩露客訴資料。
- 首次回覆時建立 CAPA，草稿可局部更新，正式提交須完整填寫 D1–D8；提交後鎖定供應商修改，需由內部品質流程將狀態退回 `revision_requested` 才能再編輯。截止日會回傳並標示逾期，但目前不拒絕逾期回覆。
- 新增 `SupplierCapaResponseRevisions` 保存每次草稿／提交時的 D1–D8 快照、供應商使用者、修訂序號及時間，避免覆蓋歷程；新增 migration `20261027170000_AddSupplierCapaResponseHistory`，目前僅在隔離 PostgreSQL 測試 schema 升級／回滾驗證，未套用到 ERP 業務資料 schema。
- CAPA 服務測試涵蓋供應商隔離、草稿修訂留存、完整提交、提交後鎖定及角色授權；新增 `SupplierCapaResponseHistoryMigrationIntegrationTests` 與 `SupplierCapaReviewAndAttachmentsMigrationIntegrationTests`，使用本機 PostgreSQL 測試連線和隨機 schema 驗證 migration 升級／回滾，測試通過後刪除該 schema。連線變數只供測試程序使用，不寫入 repository 或永久環境變數。附件實際傳輸／掃描仍須在部署 MinIO、ClamAV 後進行整合驗證；逾期通知與 Portal UI 尚未完成，不接受未經安全檢查的附件 URL 或檔案路徑。

### 第 3 階段：請款、三方勾稽與付款查詢

- 請款送審、憑證保存、補件與審核流程。
- PO、收貨／驗收與發票差異比對。
- 核准後連動正式應付帳款。
- 提供供應商專屬付款狀態查詢。
- 台灣電子發票與稅務處理另經財務及法規政策確認。

## 9. 驗收與安全測試

### 身分與隔離

- 供應商 A 嘗試讀取或修改供應商 B 的 RFQ、報價、PO、ASN、收貨、CAPA、文件、訊息、請款或 AP，不得成功。
- 將 URL、query、request body 中的 `SupplierId` 改為其他值，不能改變伺服器端授權範圍。
- 供應商 token 不能呼叫員工管理 API；員工 token 也不能繞過供應商 API 的供應商範圍檢查。
- 未核准、未啟用、停用、邀請失效、MFA 未完成或撤銷的帳號不能使用 Portal。
- 停用帳號後，舊 token／session 應依核定的撤銷策略失效。

### 業務流程

- RFQ 到期後不能提交一般報價；修訂可追溯版本與提交人。
- PO 回覆記錄對應訂單版本；正式變更仍由內部核准流程處理。
- ASN 不能超過未交數量或政策允許的容差；必填批號與效期規則依品項正確執行。
- CAPA 逾期、文件到期及提醒失敗可被追蹤。
- 三方勾稽有差異時不能自動核准 AP；核准前不得產生正式 AP 或會計過帳。

### 檔案與運維

- 附件上傳限制檔案大小與允許格式，通過惡意程式掃描，且下載前執行供應商授權。
- 日誌及錯誤訊息不洩露密碼、token、內部檔案路徑或其他供應商資料。
- APISIX、Keycloak、Portal API 及通知工作具有健康檢查、操作紀錄、失敗告警與憑證輪替程序。

## 10. 需業務與維運確認的決策

在正式對外部署與功能開發前，需確認：

1. Portal 公開網域、DNS、憑證、DMZ 網段及防火牆規則。
2. APISIX 與 Keycloak 的部署、備份、可用性、版本升級及管理責任。
3. Keycloak realm／client 拓樸、帳號命名與唯一性規則、Admin API service account 最小權限及機密管理方式。
4. 邀請連結有效期、重寄次數／冷卻時間、MFA 方法、Email／簡訊 OTP 是否允許，以及第二因素遺失的人工復原與雙人覆核程序。
5. Keycloak email verification、MFA 的 token claim／authentication flow 契約，以及首次登入完成邀請時的驗證方式。
6. 供應商帳號管理者角色、供應商內多使用者角色矩陣、同一 email 是否可屬於不同供應商的政策。
7. 供應商申請與既有供應商主檔的比對、重複統編處理及核准權責。
8. 舊本地密碼帳號停用時程、重新邀請／身分核對方式及支援流程。
9. `SupplierQuote` 與 `RfqSupplierResponse`、`SupplierAsn` 與 `Asn` 的既有資料、使用端及整併策略。
10. PO 超交容差、報價修訂、交期協商與供應商回覆的法律／採購政策。
11. 批號、製造日期、有效期限及 QR／條碼標籤的品項規則與倉庫作業格式。
12. 電子發票、營業稅、請款與付款日資訊的財務政策及對外揭露範圍。
13. 檔案儲存位置、備份與保存期限、病毒掃描、文件可見範圍及郵件服務。

## 11. 建議優先順序

建議依序完成**第 0 階段安全門檻與第 1 階段核心協作**，再擴充第 2 階段物流／品質能力。目前已開始第 2 階段，先交付重用既有採購 ASN 與收貨資料的後端切片；通過外部安全驗收前仍不得公開入口。

目前已有供應商登入／身分授權、RFQ／PO 協作、CAPA 後端及供應商 CAPA 操作介面；ASN 後端已有切片但 Portal 操作介面尚未完成。CAPA 附件與內部審查功能已建置，實際 MinIO／ClamAV 部署驗證、文件／訊息 API、正式 Keycloak realm／MFA／郵件、APISIX／DMZ 與公網 TLS 仍待完成。因此，在第 0 階段安全門檻完成前，不應把目前入口直接暴露於公網。

## 12. 供應商 Portal 前端 UI 與 Keycloak OIDC 實作規劃

### 12.0 已建立的前端基礎切片

- 已建立獨立 React／TypeScript／Vite 專案 [erp-supplier-portal](../../src/erp-supplier-portal/)，不依賴員工 ERP 的登入 provider、local storage token 或員工 API client；開發入口預設為 `http://localhost:5176`。
- 使用 `oidc-client-ts` 設定 OIDC Authorization Code Flow + PKCE `S256`、獨立登入與登出 callback、React Router 路由守衛、分頁暫存 OIDC state，以及只存在記憶體的登入使用者／access token。
- 已串接目前存在的 `/api/supplier-portal` session、邀請完成、RFQ 與 PO API；實作 Dashboard、RFQ 清單／報價、PO 清單／回覆及手機版導覽。後端 PO 摘要及明細 DTO 回傳版本與最近回覆版本供 UI 判斷待處理狀態。
- 已新增客訴／CAPA 清單與明細頁，串接供應商範圍內的客訴查詢、D1–D8 草稿儲存／送審、回覆修訂歷程及附件清單／multipart 上傳／下載／撤銷；前端依 `sales` 角色及 CAPA 狀態顯示可操作項目，實際授權仍由後端執行。送審前要求八項內容齊備且有效附件均通過掃描。
- 本機 Vite 代理 `/api/supplier-portal`，預設目標為 `http://localhost:5100`，可用開發伺服器變數 `SUPPLIER_PORTAL_API_PROXY_TARGET` 覆寫。建置與設定說明見 [Portal README](../../src/erp-supplier-portal/README.md)；公開設定範本為 [.env.example](../../src/erp-supplier-portal/.env.example)。
- 已以一次性本機 Keycloak realm 驗證真實 Authorization Code + PKCE `S256` redirect、登入 callback、邀請完成與 `/session` 供應商綁定授權；測試亦發現並修正 React StrictMode 下重複兌換 authorization code 的問題。正式 realm 的 issuer、client、MFA／audience mapper、核准 redirect URI、郵件及正式 TLS 尚待設定與驗收。Portal 靜態站、APISIX 路由或公開網域尚未部署；不得視為已完成對外上線。
- 本機另已啟動獨立的 Keycloak 26.3 開發容器 `erp-supplier-portal-keycloak-local`，僅綁定 `127.0.0.1:8182`；realm `supplier-portal`、Public Client `supplier-portal-local` 僅允許 Portal `localhost:5176` callback，啟用 PKCE `S256` 並加入開發用 `supplier-e2e-api` audience mapper。資料保存在專用 Docker volume，設定檔位於 `deployment/keycloak/`；這不是 staging／正式身分服務，亦未建立或綁定 ERP 供應商使用者。
- Portal 本機 `.env.local` 已指向 `http://localhost:8182/realms/supplier-portal`；瀏覽器已驗證登入頁能導向該 realm，並以正確 callback、state、PKCE challenge 取得 Keycloak 登入頁。現行主 ERP API 容器尚未配置 `SupplierIdentity` Authority／Audience／Admin API 設定，亦無測試供應商帳號綁定，因此這只證明 OIDC 登入入口與 Client 設定有效，不代表登入後 Portal API 或邀請流程已打通。完整設定與限制見 [本機 Keycloak 說明](../../deployment/keycloak/README.md)。

### 12.1 現況與前端隔離決策

- 現有 [SupplierPortal.tsx](../../src/erp-frontend/src/pages/SupplierPortal.tsx) 是員工 ERP 裡的頁面，包含舊本地帳密表單，並呼叫尚不存在的 `/supplier-portal/login`、`/supplier-portal/quotes/rfqs` 與 ASN API。不得沿用該登入邏輯或把它直接改成共用員工登入狀態。
- 員工 API client 以 `VITE_API_BASE_URL`（預設 `/api/erp`）及 `erp_token` 為基礎；Portal API 使用 `/api/supplier-portal/...`，並採另一套 Keycloak bearer token。兩種 client、token、登入狀態與錯誤處理必須分開，Portal 請求不可經過員工 token interceptor。
- **建議以同一 Git repository 中獨立建置／部署的 Portal SPA 作為正式方向**，並使用獨立網域／origin（例如 `portal.example.com.tw`），員工 ERP 維持內部網址。如此可避免員工頁面、模組權限、`erp_token` 和供應商登入共用同一應用程式生命週期。可重用設計 token、共用元件或 API 型別，但不共用認證儲存。
- 開發環境應為 Portal 使用獨立 Vite entry／port；後端 CORS 只加入明確的 Portal origin。正式環境經 APISIX／DMZ 導流到 Portal 靜態站及供應商 API，HTTPS、CORS origins 與 redirect URI 皆不得使用 wildcard。

### 12.2 導覽架構與畫面清單

Portal 前端目前只呈現已串接的第 1 階段功能；ASN 雖已有後端切片，尚未提供操作畫面。未實作模組應標示為尚未開放或暫不顯示，不以假資料填補：

| 畫面／路由 | 主要內容與操作 | 目前 API |
|---|---|---|
| 登入／邀請入口 | 品牌識別、Keycloak 登入、邀請後帳號設定說明、支援聯絡方式、載入／錯誤狀態 | OIDC redirect；邀請啟用 `POST /api/supplier-portal/invitations/complete` |
| 首頁 Dashboard | 待回覆 RFQ、未回覆 PO、近期截止提醒、快捷入口；統計只由供應商自己的 RFQ／PO API 計算 | `GET /api/supplier-portal/session`、`GET /api/supplier-portal/rfqs`、`GET /api/supplier-portal/purchase-orders` |
| 詢價與報價清單 | RFQ 編號、發出／截止日期、品項數、已報價數、狀態；可搜尋及切換待處理／已回覆 | `GET /api/supplier-portal/rfqs` |
| RFQ 明細／報價 | 品號、品名規格、需求量、目前自己的報價；輸入單價、交期、MOQ、貿易條件及備註後確認提交 | `GET /api/supplier-portal/rfqs/{rfqId}`、`POST /api/supplier-portal/rfqs/{rfqId}/quotes` |
| 採購單清單 | PO 編號、日期、交期、狀態、金額及最近回覆；依待確認／已回覆篩選 | `GET /api/supplier-portal/purchase-orders` |
| PO 明細／回覆 | 訂單及品項、付款／貿易條件、回覆歷程；確認接單，或逐品項提出替代數量／交期並填原因，或拒絕並填原因 | `GET /api/supplier-portal/purchase-orders/{purchaseOrderId}`、`POST /api/supplier-portal/purchase-orders/{purchaseOrderId}/responses` |
| ASN 清單／明細／建立（前端尚未完成） | 查詢自己的出貨通知、查看可出貨數量與批號規則、提交承運及追蹤資訊與出貨明細 | `GET /api/supplier-portal/asns`、`GET /api/supplier-portal/asns/{asnId}`、`GET /api/supplier-portal/purchase-orders/{purchaseOrderId}/asn-options`、`POST /api/supplier-portal/asns` |
| 客訴／CAPA | 查看自家客訴與回覆期限、保存 D1–D8 草稿、提交改善措施、管理未提交附件及查看歷次修訂；員工品質人員可審查、退回、核准、結案與管理附件 | `GET /api/supplier-portal/complaints`、`GET /api/supplier-portal/complaints/{complaintId}`、`PUT /api/supplier-portal/complaints/{complaintId}/capa`、`POST /api/supplier-portal/complaints/{complaintId}/capa/submit`、`GET/POST/DELETE /api/supplier-portal/complaints/{complaintId}/capa/attachments`、`GET /api/supplier-portal/complaints/{complaintId}/capa/attachments/{attachmentId}/content` |
| 帳號與說明 | 顯示 API `/session` 回傳的聯絡人及供應商名稱／代碼、登出、操作說明與支援方式；不可提供修改 SupplierId 或升級角色的 UI | `/session`；Keycloak logout |

共用版面建議採供應商專屬品牌列（公司標誌、供應商名稱、帳號選單）、桌面側邊導覽、手機抽屜式導覽、頁面標題與 breadcrumbs、明確的主要動作按鈕、資料載入 skeleton、空狀態、錯誤重試與成功 toast。表格在小螢幕改為資訊卡或可水平捲動，不得只靠顏色表示狀態。

### 12.3 關鍵互動與狀態設計

- **登入與邀請啟用**：Portal 初始化 OIDC client；未登入時導向 Keycloak。回到合法 callback 後，以 bearer token 呼叫邀請完成端點，再讀取 `/session`。若帳號尚未完成 email／密碼／MFA 設定，顯示可理解的下一步與重試入口；若供應商帳號未核准、已停用或邀請失效，顯示拒絕使用與聯絡窗口，不重試登入迴圈。
- **Token / API 狀態**：API 401 時清除本機 OIDC 使用者狀態並重新登入；403 顯示身分已驗證但無 Portal 權限；404 不洩露其他供應商資料；409 顯示 RFQ 截止、PO 不可回覆或版本衝突原因；網路及 5xx 提供安全的重試操作，避免重複送出。
- **RFQ 報價**：可逐品項填報；前端驗證必填、數值精度與日期格式，後端仍是最終驗證者。送出中停用重複按鈕；成功後重載明細並顯示提交時間。已截止／已關閉的 RFQ 不顯示可提交控制項，但 API 仍須維持拒絕保護。
- **PO 回覆**：接受動作顯示訂單版本及確認對話框；變更提案逐品項列出原數量／交期和替代值，理由必填；拒絕需填原因並再次確認。完成後顯示回覆歷程，正式 PO 欄位仍為唯讀。
- **CAPA 回覆與附件**：清單顯示回覆期限及 CAPA 狀態；`sales` 角色在可編輯狀態可儲存部分草稿，提交前須完整填寫 D1–D8 並確認。附件限制為每檔 20 MB、PDF／JPEG／PNG，每 CAPA 最多 10 個有效附件；安全掃描完成前不提供下載，未提交版本附件可撤銷。409／503 等狀態以明確訊息呈現，並提供重新載入附件或案件的操作。
- **角色權限**：UI 可依 `/session` 的 role 隱藏不能操作的按鈕，但這只是 UX；後端授權才是安全邊界。現有服務只有 `sales` role 可提交 RFQ／PO 回覆，需由業務確認是否納入 supplier admin 角色及只讀角色。
- **尚未完成的功能**：ASN 已有後端 API，但尚無 Portal 操作頁面；請款送審 Portal 首版頁面已加入案件清單、來源選取、草稿、附件、送審與歷程，但尚未完成瀏覽器／端到端驗收及正式啟用，付款資訊仍未實作。HTTP 授權／端到端驗收及財務／資安政策仍待確認，正式資料庫 migration 尚未套用；上述 gate 完成前不得對外開放。文件與訊息功能仍未完成。CAPA 頁面已實作，但附件實際 MinIO／ClamAV 整合與瀏覽器操作仍須在服務設定完成後驗證。未實作模組不顯示可操作入口，也不呈現無來源的付款／品質摘要。

### 12.4 Keycloak OIDC Client 與 SPA 流程

1. 在指定 realm 建立供應商專用 OIDC **public client**；SPA 不得保存 client secret。使用 Authorization Code Flow + PKCE（`S256`），停用 Implicit Flow 與 Direct Access Grants，啟用精確列舉的 Web Origins、Valid Redirect URIs 與 post-logout redirect URIs。
2. `issuer/authority` 必須與後端 `SupplierIdentity:Authority` 完全一致；access token 的 `aud` 必須包含後端設定的 `SupplierIdentity:Audience`。確認 token 含穩定 `sub`、`email_verified` 及 MFA 完成所需的 Keycloak 使用者狀態；ERP 仍以 `sub` 對應已啟用的 `SupplierUser`，不可用 email 或前端傳入 SupplierId 代替。
3. SPA 啟動後處理 redirect callback、載入 session，再呼叫 `/api/supplier-portal/session`。新邀請首次登入時呼叫 invitation completion API；後端會再向 Keycloak Admin API 核對帳號啟用、email、required actions、correlation ID 與設定要求的 TOTP credential。完成後才允許進入 Portal。
4. 使用 `oidc-client-ts` 及 React provider（建置時再確認版本相容）集中處理 redirect、state／nonce、PKCE、token 更新及登出；OIDC runtime 設定只含 issuer、client ID、API audience／scope、callback 與 logout URL 等公開值。Access token 優先只留在記憶體；若使用 refresh token，須由 Keycloak 支援並設定 rotation、reuse detection、短效期限及登出撤銷政策，不把 token 寫入 `localStorage` 或 URL。
5. 使用獨立 Portal API client，僅將 Keycloak access token 加到 `/api/supplier-portal/...` 請求；不可使用員工 `erp_token`、員工 Axios response interceptor、員工 `/api/erp` base URL，亦不可把 ID token 當 API bearer token。
6. 登出需清除 Portal OIDC client 的本地狀態並導向 Keycloak end-session endpoint；是否支援全裝置登出與 refresh token 撤銷，依 Keycloak client 設定及後端政策驗證。避免在 console、分析事件或前端錯誤訊息記錄任何 token／authorization code。

### 12.5 建議的實作批次與驗收

| 批次 | 交付內容 | 驗收 |
|---|---|---|
| A. OIDC 基礎 | 確認 realm/client、callback、audience、MFA policy；建立獨立 Portal entry、設定載入、PKCE 登入／登出與未登入路由 | 生產設定使用 HTTPS；無 client secret；state／nonce／PKCE 驗證；員工 token 不會附加到 Portal API |
| B. Session 與生命週期 | 登入 callback、邀請完成、`/session`、載入／403／邀請逾期畫面 | 有效供應商可進入；未綁定、停用、未核准、MFA 未完成及錯誤 audience 均被拒絕且 UX 明確 |
| C. RFQ UI | Dashboard 摘要、RFQ 列表／明細、報價表單、錯誤及截止狀態 | 只顯示登入供應商被指派資料；送出後能重新讀取該供應商報價；過期／未指派 API 回覆不會被 UI 繞過 |
| D. PO UI | PO 清單／明細、接受／拒絕／逐品項變更、版本與歷程 | A 供應商無法在 UI 或 API 讀取／回覆 B 的 PO；必填理由、版本衝突、重複送出及成功歷程行為符合 API |
| E. 安全與可用性驗收 | 手機／桌面 RWD、鍵盤及螢幕閱讀器基本檢查、瀏覽器刷新／逾時／登出、跨供應商與員工身分測試 | 通過 0 階段安全門檻後才部署到公網；CORS 與 APISIX 只允許核准 Portal origin／路由 |

### 12.6 開始實作前需確認的設定

- 正式 Portal hostname、開發 callback port、APISIX 路徑及 ERP API HTTPS origin。
- Keycloak realm／issuer、Portal public client ID、access-token audience／scope，以及供應商使用者的 role／MFA claim 契約。
- MFA 使用 TOTP 或其他政策允許方式、邀請完成後的首頁導向，以及邀請逾期／停用帳號的客服流程。
- `sales`、supplier admin、finance、viewer 等角色可執行的 UI 動作與 API permission matrix。
- 獨立 Vite app／entry 的部署目錄、靜態檔發布方式、公開 runtime config 注入方式，以及 CORS allowlist 的維運來源。
