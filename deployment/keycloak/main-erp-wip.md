# Main ERP isolated WIP environment

This stack is a new, loopback-only WIP for the main ERP supplier-invitation workflow. It does not reuse the existing supplier Portal WIP at `:5176/:5102/:8182`, and it never connects to the main `ErpGeneral` database or the main ERP API.

## Endpoints and isolation

| Component | WIP endpoint / resource |
|---|---|
| ERP admin frontend | `http://localhost:5177` |
| ERP API | `http://localhost:5103` |
| Supplier Portal | `http://localhost:5178` |
| WIP Keycloak | `http://localhost:8183`, realm `erp-main-wip` |
| WIP PostgreSQL | Docker container `erp-main-wip-postgres`, host port `55434`, database `ErpGeneral` |

PostgreSQL and Keycloak run in their own Docker containers and named volumes. The API and both Vite frontends run as local development processes bound to `127.0.0.1`; this keeps the OIDC issuer `http://localhost:8183/realms/erp-main-wip` reachable and identical in the browser and API token validation. The WIP backend is built in Release configuration to avoid replacing the Debug assembly used by the existing isolated WIP API. The ERP administrator login remains the ERP's own username/password flow; Keycloak is used for supplier Portal invitations and supplier OIDC.

The WIP Keycloak realm has:

- Public Portal client `erp-main-wip-supplier-portal`, Authorization Code + PKCE `S256`, exact callbacks on port `5178`, and the `supplier-portal-api` audience mapper.
- Confidential service client `erp-main-wip-backend`, with only `view-users`, `query-users`, and `manage-users` realm-management roles.
- No imported production users or credentials. Create synthetic supplier data and invitations only in this WIP database.

## Prepare and start

From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\prepare-main-erp-wip.ps1
```

The first run generates isolated PostgreSQL, ERP JWT, Keycloak admin, service-client, and initial ERP admin credentials in the Git-ignored file `deployment\keycloak\.local\main-erp-wip.env`. Do not commit or share this file. The script starts only the two new containers, provisions the WIP realm/clients, applies the full EF migration chain to the new empty WIP database, and builds the backend.

Start each local process in a separate PowerShell terminal:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\run-main-erp-wip.ps1 -Service api
powershell -ExecutionPolicy Bypass -File deployment\keycloak\run-main-erp-wip.ps1 -Service admin
powershell -ExecutionPolicy Bypass -File deployment\keycloak\run-main-erp-wip.ps1 -Service supplier-portal
```

The API seeds an ERP administrator on first startup. Use username `admin`; the generated initial password is stored only in the ignored local credentials file. To view it locally in PowerShell, without copying it into chat:

```powershell
(Get-Content deployment\keycloak\.local\main-erp-wip.env | Where-Object { $_ -like 'MAIN_ERP_WIP_ADMIN_INITIAL_PASSWORD=*' }) -replace '^MAIN_ERP_WIP_ADMIN_INITIAL_PASSWORD=', ''
```

This is the ERP WIP password, not a Keycloak password. The account requires a password change at first login. Do not copy that account to the main database or share the generated password.

## Enable ERP user creation in this WIP

The ERP user-provisioning workflow intentionally requires each ERP user to be linked to an eligible active employee; it does not create standalone accounts. A fresh WIP database has no employee profiles, so **建立使用者** stays disabled until an eligible employee exists.

Seed one clearly synthetic, repeatable employee profile in this isolated WIP database:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\seed-main-erp-wip-user-provisioning.ps1
```

The script verifies the exact `erp-main-wip-postgres` container, loopback port `55434`, and `ErpGeneral` database before writing. It creates or updates `WIP-DEMO-001` in the WIP department and job title, using a reserved `.invalid` email and a document identifier explicitly marked as synthetic. It does not create an ERP login account. Refresh `http://localhost:5177/` after running it; `WIP-DEMO-001 - WIP 測試員工` will be selectable in **系統設定 → 權限管理 → 使用者 → 建立使用者**. The employee is for WIP only and must not be copied to the main database.

The frontend package dependencies must already be installed in `src\erp-frontend` and `src\erp-supplier-portal`. If they are not, run `npm ci` in each directory; no dependency changes are required by this WIP.

Stop each API/frontend process with `Ctrl+C`. Stop only the new containers, preserving their isolated data, with:

```powershell
docker compose --env-file deployment\keycloak\.local\main-erp-wip.env -p erp-main-wip -f deployment\keycloak\docker-compose.main-erp-wip.yml down
```

Do not use `down -v` unless you intend to delete the dedicated WIP database and Keycloak realm.

## Validation and limits

After startup, verify:

- `http://localhost:5103/health` reports a connected database.
- `http://localhost:5177` loads the ERP admin frontend and its API requests go to port `5103`.
- `http://localhost:5178` redirects to the `erp-main-wip` realm at port `8183`.
- The ERP login is the local WIP `admin` account; supplier OIDC users and invitations are managed in the separate WIP realm.

## Current local status

The isolated stack is running. The new database has 35 EF migrations applied through `20261027180000_AddSupplierApSubmissions` and contains no supplier-user rows. The API health check, ERP admin frontend, Supplier Portal, and WIP Keycloak realm returned HTTP 200. The generated ERP admin login succeeded through the admin frontend proxy; an unauthenticated supplier session returned 401; the browser's supplier-login button redirected to the `erp-main-wip` Keycloak realm using PKCE.

The backend Release build succeeded. The focused supplier invitation, identity, and Keycloak client tests passed (17/17). Both frontend builds and test suites passed (admin: 21 tests; Supplier Portal: 8 tests). The full backend test suite had one failure in the unrelated `Dtos_ShouldBeInDtosNamespace` architecture test for the existing `CapaAttachmentDto` and `CapaAttachmentUploadDto` namespaces; 506 other tests passed.

The WIP user-provisioning prerequisite is now seeded: `WIP-DEMO-001` is active, has the required synthetic document identifier, hire date, active WIP department and job title, and is not linked to an ERP account. The seed script is idempotent. Refresh the admin page to load the eligible employee into the create-user form.

Supplier approval in this WIP is available to accounts with the active `Admin` role; other accounts are subject to the existing supplier RBAC grants. The API derives an approver's employee ID from the authenticated token rather than trusting a browser-supplied ID. An admin account without an employee link may still approve; the API audit log records the authenticated ERP user.

This stack does not start the main ERP containers, the existing supplier Portal WIP, or any production/staging service. The main database migrations were separately applied to `ErpGeneral` after explicit approval; this new WIP uses its own empty database and its own migration history.
