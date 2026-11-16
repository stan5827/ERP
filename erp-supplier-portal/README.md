# ERP Supplier Portal

獨立的供應商入口 SPA，與員工 ERP 前端分開建置、路由及管理登入狀態。Portal 使用 Keycloak OIDC Authorization Code Flow + PKCE；只呼叫 `/api/supplier-portal`，不讀取或重用員工端的 `erp_token`。

## 本機啟動

1. 本機測試 realm 與 client 已由 [local Keycloak Compose](../../deployment/keycloak/README.md) 建立；本機公開 OIDC 設定已放在 `.env.local`。此檔只包含 issuer、public client ID 等公開值，不要在此設定或提交 client secret。
2. 在 Keycloak 的供應商 realm 建立 public client，啟用 Standard Flow，停用 Implicit Flow 與 Direct Access Grants；要求 PKCE method `S256`。
3. Portal 使用獨立的 `http://localhost:5176`，不取代 Release Candidate 員工 ERP 的 `http://localhost:5175`。本機 Keycloak realm 的 issuer 為 `http://localhost:8182/realms/supplier-portal`；redirect URI 為 `http://localhost:5176/auth/callback` 和 `http://localhost:5176/auth/logout-callback`，Web Origin 為 `http://localhost:5176`。
4. 確認 access token 的 `aud` 包含後端 `SupplierIdentity:Audience`，並由管理者透過邀請流程建立有效的供應商帳號。Portal 不會建立密碼或授權 SupplierId。
5. 確認含有供應商 Portal API 的 ERP API 在 `http://localhost:5100` 執行，然後執行：

   ```powershell
   npm install
   npm run dev
   ```

6. 開啟 `http://localhost:5176`。Vite 僅將 `/api/supplier-portal` 路由代理至本機 ERP API。若使用不同 API 位址，設定 `SUPPLIER_PORTAL_API_PROXY_TARGET` 後重新啟動 Vite。

若尚未設定 `VITE_KEYCLOAK_AUTHORITY` 及 `VITE_KEYCLOAK_CLIENT_ID`，Portal 會顯示設定提示而不會開始登入。本機開發伺服器的預設 API proxy 是 `http://localhost:5100`；Release Candidate staging 後端 `http://localhost:5101` 不含目前的供應商 Portal API，因此不可直接將 proxy 指向 staging 後端。

## 公開設定

| 變數 | 說明 |
|---|---|
| `VITE_KEYCLOAK_AUTHORITY` | Keycloak OIDC issuer URL，需與後端 `SupplierIdentity:Authority` 一致 |
| `VITE_KEYCLOAK_CLIENT_ID` | Portal 專用 public client ID；不得使用 confidential client |
| `VITE_KEYCLOAK_SCOPE` | OIDC scope，預設 `openid profile email` |
| `VITE_API_BASE_URL` | Portal API base URL，預設 `/api/supplier-portal` |
| `SUPPLIER_PORTAL_API_PROXY_TARGET` | 僅供 Vite 開發伺服器使用的 Portal API 代理目標，預設 `http://localhost:5100`；不會注入瀏覽器程式碼 |

`VITE_` 變數會進入瀏覽器靜態程式碼，因此只可放公開設定。禁止放置 client secret、Admin API 憑證或其他機密。正式環境須使用 HTTPS、精確設定 redirect URI／Web Origin，並由 APISIX 將同一 Portal origin 的 `/api/supplier-portal` 導向 ERP 後端。

本機 HTTP Keycloak 測試僅限後端 `Development` 環境、明確啟用 `SupplierIdentity:AllowHttpMetadata`，且 Authority 與 Admin API 都是 loopback URI；其他環境一律要求 HTTPS。

## 功能現況

- 已串接 RFQ／PO 操作、CAPA 回覆與附件，以及 AP Submission 請款清單、來源選取、草稿、附件安全狀態、送審及審核歷程。
- AP Submission 不包含付款進度查詢；核准不代表已付款。業務資料庫的 migration、附件服務正式設定、Keycloak 授權與端到端驗收完成前，不應對外啟用請款流程。
- ASN、文件與訊息功能尚未提供完整的 Portal 操作頁面。

## 登入與安全界線

- OIDC `response_type=code`、PKCE 啟用（S256）；OAuth state 暫存於分頁 session storage，登入使用者及 access token 僅存在記憶體。不使用 local storage，也不啟用 silent token renewal；完整重新整理頁面後需重新導向 Keycloak 建立工作階段。
- Access token 只附加到 Portal API 請求；API 401／403／404／409 會依狀況回報，不會嘗試員工端登入或重用員工 token。
- Portal 路由由自己的 React Router 及登入 guard 保護。伺服器仍以 Keycloak `sub` 對應已啟用的 ERP `SupplierUser`，前端路由不是資料授權邊界。
- 登入完成後，Portal 呼叫邀請完成端點及 `/session`。後端負責驗證供應商核准狀態、email 驗證、邀請 correlation 與 TOTP 設定。
- 本機 Keycloak realm 僅供 OIDC／PKCE 開發，不會自動建立 ERP 供應商使用者或授權資料；供應商範圍、狀態、附件掃描及請款核准仍須由後端驗證。不得以假資料填補未實作模組。

## 驗證

```powershell
npm test
npm run build
```
