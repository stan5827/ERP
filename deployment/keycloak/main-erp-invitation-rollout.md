# Main ERP Supplier Portal Invitation Rollout

## Current state

The workspace source contains the Keycloak invitation UI and backend flow. The UI collects email, contact name, and a Portal role; it does not create local supplier passwords. The currently running `erp-frontend:latest` and `erp-backend:latest` containers were built before this source was deployed, which explains why `http://localhost:5173/` still shows the legacy username/password form and returns a required-`ContactName` validation error.

The main backend currently runs against the business database. The two approved supplier invitation migrations have been applied to `ErpGeneral`; the backend and frontend containers have not been restarted or replaced. The local Keycloak realm and WIP test credentials are not production identity configuration and must not be copied into the main ERP environment.

## Prepared changes

- `deployment/docker-compose.yml` now passes optional `SupplierIdentity` configuration to the backend container.
- `deployment/docker-compose.env.example` documents the non-secret settings and marks the client secret as a secret-store value.
- The main ERP must receive an HTTPS authority reachable from the backend container. Its URL must exactly match the OIDC token `iss` claim. The Keycloak Admin API URL must also be reachable from the backend container.
- The confidential service client should be restricted to the Keycloak realm roles needed to find, create, update, and disable supplier users; never expose its secret to the browser.
- Do not set `SupplierIdentity__AllowHttpMetadata=true` in Production. The implementation only permits HTTP metadata in Development with a loopback provider.

## Database migration status

The source model and invitation workflow required these migrations on the **main business database**:

1. `20261027121100_AddSupplierIdentityProviderSubject` — adds nullable `SupplierUsers.IdentityProviderSubject` and a unique index.
2. `20261027121800_AddSupplierPortalInvitationLifecycle` — adds invitation state, correlation, expiry and retry columns plus a unique correlation index.

`ContactName` already exists on the supplier user model; the screenshot's validation error is from the old UI/API version, not a missing migration for that field.

**Applied (2026-10-28, after explicit user approval):** a pre-migration backup was created as `ErpGeneral-pre-supplier-portal-invitation-20261028.dump` in the session files area. The migration SQL was generated offline and applied directly to Docker container `erp-postgres`, database `ErpGeneral`, after verifying that the latest migration was `20261027020000_AddManualAccountingVoucherWorkflow` and neither target migration was already recorded.

**Post-apply verification:** both migration IDs are recorded in `public."__EFMigrationsHistory"`. The provider-subject and invitation lifecycle columns exist; the two expected unique indexes exist; `SupplierUsers` contains zero rows. No supplier, invitation, or WIP test data was inserted. The generated SQL and its exact staged copy in the container were removed after application.

## Coordinated deployment sequence (remaining work)

1. Configure the main Keycloak realm/client, supplier Portal redirect URIs, API audience, email delivery, required actions, and restricted confidential service account.
2. Store the main ERP identity settings from `deployment/docker-compose.env.example` in the approved secret/configuration system. Do not use `deployment/keycloak/.env.local`.
3. Build and deploy backend and frontend images from the same workspace revision. Updating only one image leaves the account modal and API contract mismatched.
4. Verify the backend health endpoint, the supplier-invitation API, rejection of missing/invalid identity configuration, and an invitation to a specifically approved test supplier/contact.
5. Confirm the invitation email, verified-email/password/MFA onboarding, subject binding, authorized Portal session, and cancellation behavior. Verify that no password is created or accepted by the ERP API.
6. Reconcile the test supplier and invitation records, then remove or disable them according to the agreed production test-data policy.

## Current validation boundary

The Keycloak invitation implementation and tests are present in the workspace; the running main ERP images are not yet updated. The main backend is healthy, but its runtime has no `SupplierIdentity` authority, audience, Admin API URL, or service-client credentials configured. Coordinated deployment therefore remains blocked on main-provider configuration. The local WIP environment is a separate test setup and is not evidence that the main ERP identity provider is configured.
