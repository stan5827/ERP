# Supplier Portal AP Submission WIP Test Record

**Test date:** 2026-10-28  
**Environment:** Local WIP database, isolated Supplier Portal API, local Keycloak  
**Outcome:** Positive AP submission flow completed through internal review queue

## Scope and isolation

This run validated the supplier-facing flow from an eligible purchase order and accepted goods receipt through AP draft creation, attachment scanning, and submission. It used the synthetic `SUP-TEST-PORTAL` supplier only.

The AP data seeder is hard-bound to the `erp-wip-candidate-test-db-20261025` Docker container and verifies the loopback mapping to port `55432` and database name `ErpGeneral` before writing. The isolated API uses port `5102`; Keycloak, MinIO, and ClamAV use loopback ports `8182`, `9000`, and `3310`. The main ERP API on port `5100` was not restarted or targeted by the seeder.

Credentials and generated MinIO credentials remain in the Git-ignored `deployment/keycloak/.env.local`. Do not copy credential values, access tokens, or real supplier documents into this record or source control. The local invoice attachment used in this test was synthetic and explicitly marked as test-only.

## Replaying the setup

From the repository root, start the isolated attachment services, build the backend if needed, start the WIP-only API, and seed the synthetic PO/GR:

```powershell
powershell -ExecutionPolicy Bypass -File deployment\keycloak\start-supplier-portal-attachment-services.ps1
dotnet build src\ErpBackend\ErpBackend.csproj
powershell -ExecutionPolicy Bypass -File deployment\keycloak\run-supplier-portal-test-backend.ps1
powershell -ExecutionPolicy Bypass -File deployment\keycloak\seed-supplier-portal-ap-test-data.ps1
```

The attachment service startup script stores any newly generated MinIO password only in the ignored local environment file. Restart the isolated API after starting the attachment services so it loads the scanner and object-storage configuration. Never use these local settings for staging or production.

The seeder creates or updates:

| Record | Identifier | Relevant test state |
|---|---|---|
| Supplier | `SUP-TEST-PORTAL` | Existing active, approved synthetic supplier |
| Purchase order | `PO-PORTAL-TEST-2026-001` | `confirmed`, unlocked, TWD 250 |
| Goods receipt | `GR-PORTAL-TEST-2026-001` | `received`, IQC `passed`, accepted quantity 2 |
| SKU / warehouse | `FG-001` / `FW` | Reused existing WIP records |

The fixed document identifiers and transactional upserts make repeated seeder runs idempotent. The script does not add a duplicate accounting voucher or journal schema.

## Browser flow and results

1. Sign in at the local Supplier Portal using the supplier test account. Do not use the Keycloak administrator account for supplier flows.
2. Open **請款送審** → **新增請款**. Confirm that the eligible source is `PO-PORTAL-TEST-2026-001 / GR-PORTAL-TEST-2026-001` and that two `FG-001` units are available.
3. Create a draft using synthetic invoice `INV-PORTAL-TEST-2026-001`, date `2026-10-28`, quantity `2`, and total TWD 250.
4. Upload a synthetic PDF clearly labelled **TEST ONLY – NOT A TAX INVOICE**. Confirm the application reports the attachment scan as clean.
5. Submit and confirm the warning. Verify the case moves to **審核中** and the source/amount match is reported.

Observed result:

- AP case: `APS-20261028-85f7e5be08b54f269`
- Persisted state in WIP: `submitted`
- Invoice amount / tax / total: TWD 250 / TWD 0 / TWD 250
- Submission lines: one line, two units
- Active attachment: one, scan status `clean`
- Portal displayed: **審核中** and **來源與金額核對相符**

This is a supplier submission only. It does not mean the internal AP review approved the case or that payment was created.

## Verification evidence and limitations

- The seed script ran twice successfully; WIP retained one matching PO and one matching goods receipt.
- The isolated API health endpoint returned HTTP 200; an unauthenticated AP eligible-source request returned HTTP 401.
- MinIO health returned HTTP 200 and ClamAV reported healthy. The uploaded synthetic PDF received a clean scan.
- WIP read-back confirmed the submitted AP case, amount, quantity, and clean attachment.
- The temporary local PDF was removed after upload. The private MinIO object is retained as part of the test attachment record.
- A final read-only query against the main ERP database could not run because PostgreSQL reported that the server had reached its client connection limit. No final independent main-database count is claimed here. The seeder only targets the named WIP container; repeat the main-database read-only check after the connection limit clears if explicit reconciliation evidence is required.
- Script syntax, Docker Compose configuration, and the `.env.local` ignore rule were verified.

## Local service lifecycle

The attachment service compose project is `erp-supplier-portal-attachments`. To stop its containers without deleting local test files or ClamAV signatures:

```powershell
docker compose --env-file deployment\keycloak\.env.local -p erp-supplier-portal-attachments -f deployment\keycloak\docker-compose.attachments.local.yml down
```

Do not add `-v` unless you intentionally want to delete the persistent local attachment objects and scanner signatures. Do not stop or remove the shared WIP database, Keycloak, or other ERP containers as part of attachment-service cleanup.

## Related scripts and setup

- [Local setup README](README.md)
- [WIP AP source seeder](seed-supplier-portal-ap-test-data.ps1)
- [Seed SQL](seed-supplier-portal-ap-test-data.sql)
- [Attachment service startup](start-supplier-portal-attachment-services.ps1)
- [Attachment service Compose file](docker-compose.attachments.local.yml)
- [Isolated API startup](run-supplier-portal-test-backend.ps1)
