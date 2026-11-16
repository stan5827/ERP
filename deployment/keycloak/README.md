# Local supplier Portal Keycloak

This is a local-only development identity server. It is not a production or staging realm and must not be exposed outside the developer machine.

## Start

Use Keycloak 26.3 bound only to `127.0.0.1:8182`. In PowerShell, set a private bootstrap administrator password in the current shell, then start the stack:

```powershell
$env:KC_BOOTSTRAP_ADMIN_USERNAME = "admin"
$env:KC_BOOTSTRAP_ADMIN_PASSWORD = "<generate a unique local password>"
docker compose -p erp-supplier-portal-keycloak -f deployment/keycloak/docker-compose.local.yml up -d
```

After the local secret file has been created, subsequent starts can load the rotated administrator credential with:

```powershell
docker compose --env-file deployment\keycloak\.env.local -p erp-supplier-portal-keycloak -f deployment\keycloak\docker-compose.local.yml up -d
```

The realm import creates:

- Realm: `supplier-portal`
- Public OIDC client: `supplier-portal-local`
- Authorization Code flow with PKCE `S256`; implicit flow, password grant, and self-registration are disabled.
- Exact callback URLs at `http://localhost:5176/auth/callback` and `http://localhost:5176/auth/logout-callback`; web origin `http://localhost:5176`.
- Access-token audience mapper for the isolated development audience `supplier-e2e-api`.

The local issuer is `http://localhost:8182/realms/supplier-portal`. Portal public settings are in its ignored `.env.local` file. No client secret is used by the SPA.

Keycloak data persists in the `supplier_portal_keycloak_data` Docker volume. `docker compose down` stops and removes the container but keeps the realm data. Do not use `docker compose down -v` unless you intend to delete that local identity data.

## Isolated ERP API and test account

The local end-to-end test setup uses the WIP test database container `erp-wip-candidate-test-db-20261025` on port `55432` and a separate backend on `http://localhost:5102`. It does not configure or restart the main ERP backend on port `5100`, and it must not be pointed at the business database.

The WIP test database has the supplier identity, invitation lifecycle, collaboration, CAPA, and AP submission migrations applied. It contains a synthetic supplier (`SUP-TEST-PORTAL`) and the `supplier.demo` test user linked to the local realm subject. The test account is active and uses the `sales` Portal role.

The confidential Keycloak client `supplier-portal-backend-local` is dedicated to this local realm. Its service account has only the `view-users`, `query-users`, and `manage-users` realm-management roles needed by the supplier identity service. The SPA continues to use the public `supplier-portal-local` client and has no client secret.

Local administrator, service-client, and test-user credentials are stored in the ignored `deployment\keycloak\.env.local` file. Do not commit or share that file. Start the isolated API from the repository root with:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\start-supplier-portal-attachment-services.ps1
dotnet build src\ErpBackend\ErpBackend.csproj
powershell -ExecutionPolicy Bypass -File deployment\keycloak\run-supplier-portal-test-backend.ps1
```

The attachment-services script generates a local MinIO password if needed and stores it only in the ignored `.env.local` file. It starts MinIO and ClamAV with ports bound to loopback (`9000` and `3310`) and creates the private test bucket. The API startup script loads the local service-client and MinIO credentials without printing them, configures the scanner and object storage, and binds the API to loopback port `5102`. Restart the isolated API after starting the attachment services. The Portal dev server on port `5176` proxies supplier API calls to this isolated backend. The test user's password is available only in the ignored local secret file under `SUPPLIER_PORTAL_TEST_PASSWORD`.

The local MinIO data and ClamAV signature database persist in Docker volumes. Do not use `docker compose down -v` unless you intend to delete those local test files and signatures.

To create repeatable synthetic purchase-order and received-goods data for AP submission testing, run this from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\seed-supplier-portal-ap-test-data.ps1
```

The script targets only the named WIP database container and verifies its loopback port (`55432`) and database name before writing. It creates or updates `PO-PORTAL-TEST-2026-001` and `GR-PORTAL-TEST-2026-001` for `SUP-TEST-PORTAL`, using the existing `FG-001` SKU and active `FW` warehouse. The purchase order is confirmed and unlocked; its goods receipt is received with passed IQC and two accepted units, making the receipt available to the Portal's AP-source lookup. Re-running the script is safe. It does not configure or write to the main ERP database.

The completed browser validation, result identifiers, verification evidence, known limitations, and replay steps are archived in [supplier-portal-ap-submission-test-record-2026-10-28.md](supplier-portal-ap-submission-test-record-2026-10-28.md).

The main ERP supplier-invitation migrations have been applied to `ErpGeneral` after explicit approval. The main ERP backend and frontend have not been redeployed because main Keycloak configuration is not present. Current status and remaining rollout steps are documented in [main-erp-invitation-rollout.md](main-erp-invitation-rollout.md). Do not reuse local WIP credentials in the main environment.

A separate, fresh WIP stack for the main ERP invitation workflow is documented in [main-erp-wip.md](main-erp-wip.md). It uses its own database, Keycloak realm, and loopback ports; it does not reuse the existing supplier Portal WIP or connect to the main database.

This is a local development setup only. A successful Keycloak sign-in alone does not grant access: the ERP must validate the token and resolve its `sub` to an active, approved supplier user. Do not copy these settings or test credentials to staging or production.
